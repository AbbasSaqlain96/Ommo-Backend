using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers;
using OmmoBackend.Models;
using OmmoBackend.Parsers;
using OmmoBackend.Repositories.Interfaces;
using System.Text.Json;
using static OmmoBackend.Parsers.IntegrationEmailParser;

namespace OmmoBackend.Services.Implementations
{
    public class IntegrationEmailPollerService : BackgroundService
    {
        private readonly ILogger<IntegrationEmailPollerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ISecretProtector _protector;
        public IntegrationEmailPollerService(
            ILogger<IntegrationEmailPollerService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ISecretProtector protector)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config;
            _protector = protector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IntegrationEmailPollerService started.");
            var pollInterval = TimeSpan.FromMinutes(_config.GetValue<int>("EmailSettings:PollMinutes", 10));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollOnce(stoppingToken);
                }
                catch (OperationCanceledException) { /* shutting down */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in IntegrationEmailPollerService loop.");
                }

                await Task.Delay(pollInterval, stoppingToken);
            }
        }

        private async Task PollOnce(CancellationToken cancellationToken)
        {
            var host = _config["EmailSettings:ImapHost"];
            var port = _config.GetValue<int>("EmailSettings:ImapPort");
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];
            var useSsl = _config.GetValue<bool>("EmailSettings:UseSsl", true);

            using var client = new ImapClient();
            await client.ConnectAsync(host, port, useSsl, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            _logger.LogInformation("Total messages: {Total}, Recent: {Recent}", inbox.Count, inbox.Recent);

            // Search UNSEEN; tweak if you want only those not processed
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
            _logger.LogInformation("Total messages: {Total}, Recent: {Recent}", inbox.Count, inbox.Recent);

            _logger.LogInformation("Found {count} unseen messages", uids.Count);

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                _logger.LogInformation("Fetched email Subject: {Subject}, From: {From}, MessageId: {MessageId}", message.Subject, message.From, message.MessageId);

                var messageId = message.MessageId ?? Guid.NewGuid().ToString();

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

                if (await repo.IsMessageProcessedAsync(messageId))
                {
                    _logger.LogInformation("Message {id} already processed. Marking as seen.", messageId);
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                    continue;
                }

                var subject = message.Subject ?? "";
                var bodyText = message.TextBody ?? message.HtmlBody ?? message.ToString();

                var parsed = IntegrationEmailParser.Parse(subject, bodyText);
                if (parsed == null)
                {
                    _logger.LogInformation("Unrecognized email format for message {id} subject: {sub}", messageId, subject);
                    // Optionally mark processed to avoid repeated checks; here we mark as seen but do NOT mark processed so devs can review
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                    continue;
                }

                // Find Integration record. Try using IntegrationID key if present else some mapping

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Extract sender email
                var fromEmail = message.From.Mailboxes.FirstOrDefault()?.Address;


                var integration = await FindIntegrationAsync(db, parsed, fromEmail);

                if (integration == null)
                {
                    _logger.LogWarning("No Integration record matched for message {id} provider {provider} customer {customer}.", messageId, parsed.Provider, parsed.Fields.GetValueOrDefault("Customer"));
                    // mark seen to avoid reprocessing; optionally store for manual review
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                    continue;
                }

                // apply changes:
                if (parsed.Success)
                {
                    integration.integration_status = "active";
                    // Build credentials JSON ensuring password is encrypted
                    var credentialDict = new Dictionary<string, string>();

                    if (parsed.Provider == "Truckstop")
                    {
                        if (parsed.Fields.TryGetValue("IntegrationID", out var intId)) credentialDict["IntegrationID"] = intId;
                        if (parsed.Fields.TryGetValue("Username", out var u)) credentialDict["Username"] = u;
                        if (parsed.Fields.TryGetValue("Password", out var pass))
                        {
                            credentialDict["Password"] = _protector.Protect(pass);
                        }
                    }
                    else if (parsed.Provider == "DAT")
                    {
                        if (parsed.Fields.TryGetValue("ServiceAccountEmail", out var se)) credentialDict["ServiceEmail"] = se;
                        if (parsed.Fields.TryGetValue("Password", out var pass))
                        {
                            credentialDict["Password"] = _protector.Protect(pass);
                        }
                    }

                    // convert to JSON document
                    integration.credentials = JsonDocument.Parse(JsonSerializer.Serialize(credentialDict));
                    integration.last_updated = DateTime.UtcNow;
                }
                else
                {
                    integration.integration_status = "inactive";
                    // put reason into extra_config.reason
                    var extra = new Dictionary<string, object>();
                    if (integration.extra_config != null) // merge existing
                    {
                        var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(integration.extra_config.RootElement.GetRawText()) ?? new();
                        foreach (var kv in existing) extra[kv.Key] = kv.Value;
                    }
                    extra["reason"] = parsed.Fields.GetValueOrDefault("Reason", "Rejected without reason");
                    integration.extra_config = JsonDocument.Parse(JsonSerializer.Serialize(extra));
                    integration.last_updated = DateTime.UtcNow;

                    // Enqueue rejection email to Requested_By_Email
                    var sendEmail = new SendEmail
                    {
                        send_to = integration.requested_by_email ?? parsed.Fields.GetValueOrDefault("Customer"),
                        subject = $"Integration Request Rejected for {parsed.Fields.GetValueOrDefault("Customer")}",
                        status = "queued",
                        created_at = DateTime.UtcNow
                    };
                    await repo.EnqueueSendEmailAsync(sendEmail);
                }

                // persist changes to Integrations
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync(cancellationToken);

                // Mark message processed
                await repo.MarkEmailProcessedAsync(messageId);

                // Mark message seen in IMAP
                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);
        }


        private async Task<Integrations?> FindIntegrationAsync(AppDbContext db, ParsedResult parsed, string? fromEmail)
        {
            Integrations? integration = null;

            // Step 1: Try IntegrationID
            if (parsed.Provider == "Truckstop" && parsed.Success && parsed.Fields.TryGetValue("IntegrationID", out var id) && int.TryParse(id, out var vendorIntegrationId))
            {
                var global = await db.global_integration_credentials
                    .FirstOrDefaultAsync(g =>
                        g.credential_name == "Truckstop_IntegrationID" &&
                        g.credential_value == vendorIntegrationId.ToString());

                if (global != null)
                {
                    integration = await db.integrations
                        .FirstOrDefaultAsync(i => i.default_integration_id == global.default_integration_id);
                }
            }

            // Step 2: Fallback → requested_by_email (using fromEmail)
            if (integration == null && !string.IsNullOrEmpty(fromEmail))
            {
                integration = await db.integrations
                    .FirstOrDefaultAsync(i => i.requested_by_email == fromEmail);
            }

            // Step 3: Fallback → Customer name (optional, only if you want extra matching)
            if (integration == null && parsed.Fields.TryGetValue("Customer", out var customer) && !string.IsNullOrEmpty(customer))
            {
                // Log only — don’t rely on it if you don’t store customers
                _logger.LogInformation("No integration found by IntegrationID or email, falling back to Customer name: {Customer}", customer);
            }

            return integration;
        }
    }
}
