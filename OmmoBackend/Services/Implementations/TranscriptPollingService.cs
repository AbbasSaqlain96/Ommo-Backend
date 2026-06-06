using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Hubs;
using OmmoBackend.Models;
using OmmoBackend.Services;  // AiInsightsService
using OmmoBackend.Dtos;
using OmmoBackend.Services.Interfaces;
using MailKit;
using static System.Net.Mime.MediaTypeNames;
using Npgsql;
//IAiInsightsService ai;// ConfirmDataDto, SentimentDto, SummaryBulletDto

namespace OmmoBackend.Services.Implementations
{
    public class TranscriptPollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;
        private readonly ILogger<TranscriptPollingService> _log;
        private readonly IHubContext<CallTranscriptHub> _hub;

        public TranscriptPollingService(
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory http,
            IConfiguration cfg,
            ILogger<TranscriptPollingService> log,
            IHubContext<CallTranscriptHub> hub)
        {
            _scopeFactory = scopeFactory;
            _http = http;
            _cfg = cfg;
            _log = log;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // create scope for scoped services
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ai = scope.ServiceProvider.GetRequiredService<IAiInsightsService>();
                var hub = scope.ServiceProvider.GetRequiredService<IHubContext<CallTranscriptHub>>();

                // 🔹 IMPORTANT: same filter you had – do NOT touch transcript flow
                var calls = await db.call
                     .Where(c =>
                         c.is_transcript_complete == false &&           // only incomplete transcripts
                         (
                             c.status_of_call == "live" ||
                             (c.status_of_call == "ended" &&
                              c.call_end_time > DateTime.UtcNow.AddMinutes(-5))
                         ))
                     .ToListAsync(stoppingToken);

                foreach (var call in calls)
                {
                    try
                    {

                        await FetchAndBroadcastTranscriptAsync(call, db, hub, stoppingToken);
                        await BroadcastAiProcessingStatusAsync(call, hub, stoppingToken);

                        var now = DateTime.UtcNow;
                        if (call.is_ai_processing_complete)
                            continue;

                        bool shouldRunAi = false;
                        if (call.status_of_call == "ended" && call.is_transcript_complete)
                        {
                            shouldRunAi = true;
                        }
                        else
                        {
                            if (call.last_ai_processed == null ||
                                call.last_ai_processed <= now.AddSeconds(-10))
                            {
                                shouldRunAi = true;
                            }
                        }

                        if (!shouldRunAi)
                            continue;

                        // 3) Run AI insights for this call
                        await ProcessInsightsAsync(call, stoppingToken, db);

                        // 4) Update AI timing markers
                        call.last_ai_processed = now;

                        // If this was the final pass (ended + transcript complete), mark complete
                        if (call.status_of_call == "ended" && call.is_transcript_complete)
                        {
                            call.is_ai_processing_complete = true;
                            call.last_ai_processed = now;
                        }

                        await db.SaveChangesAsync(stoppingToken);
                        await BroadcastAiProcessingStatusAsync(call, hub, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Transcript/AI processing failed for call {CallId}", call.call_id);
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
            }
        }

        private static Task BroadcastAiProcessingStatusAsync(
            Call call,
            IHubContext<CallTranscriptHub> hub,
            CancellationToken ct)
        {
            return hub.Clients
                .Group(call.call_id.ToString())
                .SendAsync("Aiproccess", new
                {
                    call_id = call.call_id,
                    is_ai_processing_complete = call.is_ai_processing_complete,
                    timestamp = DateTime.UtcNow,
                }, ct);
        }

        async Task FetchAndBroadcastTranscriptAsync(
     Call call,
     AppDbContext db,
    IHubContext<CallTranscriptHub> hub,
    CancellationToken ct)
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Remove("X-API-Key");
            client.DefaultRequestHeaders.Add("X-API-Key", _cfg["Ultravox:ApiKey"]);

            var resp = await client.GetAsync(
                $"https://api.ultravox.ai/api/calls/{call.caller_id}/messages", ct);

            if (!resp.IsSuccessStatusCode)
                return;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var messages = doc.RootElement
                .GetProperty("results")
                .EnumerateArray()
                .ToList();

            if (messages.Count == 0)
                return;

            int lastFetchedIndex = call.last_uvx_index_fetched ?? -1;

            var newMessages = messages
                .Where(m => m.GetProperty("callStageMessageIndex").GetInt32() > lastFetchedIndex)
                .OrderBy(m => m.GetProperty("callStageMessageIndex").GetInt32())
                .ToList();

            if (!newMessages.Any())
                return;

            // 🔑 Advance cursor IMMEDIATELY (prevents retries)
            int maxIncomingIndex = newMessages
                .Max(m => m.GetProperty("callStageMessageIndex").GetInt32());

            call.last_uvx_index_fetched = maxIncomingIndex;
            await db.SaveChangesAsync(ct);

            foreach (var msg in newMessages)
            {
                int index = msg.GetProperty("callStageMessageIndex").GetInt32();
                string? text = msg.GetProperty("text").GetString();
                string role = msg.GetProperty("role").GetString() ?? "";
                string speaker = role == "MESSAGE_ROLE_AGENT" ? "Bot" : "Broker";

                // 🔥 1️⃣ SIGNALR FIRST (LIVE)
                await hub.Clients
                    .Group(call.call_id.ToString())
                    .SendAsync("ReceiveTranscriptLine", new
                    {
                        call_id = call.call_id,
                        speaker,
                        text,
                        timestamp = DateTime.UtcNow,
                        message_index = index
                    }, ct);

                // 💾 2️⃣ DB SECOND (BEST EFFORT)
                try
                {
                    db.call_transcript.Add(new CallTranscript
                    {
                        call_id = call.call_id,
                        speaker = speaker,
                        text = text,
                        message_index = index,
                        timestamp = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (
                    ex.InnerException is PostgresException pg &&
                    pg.SqlState == "23505")
                {
                    // duplicate → ignore (already stored)
                }
            }
        }



        private async Task ProcessInsightsAsync(Call call, CancellationToken ct, AppDbContext _db)
        {
            try
            {
                // ---------------------------
                // GET FULL TRANSCRIPT
                // ---------------------------
                List<string> fullTranscript;
                try
                {
                    fullTranscript = await _db.call_transcript
                        .Where(t => t.call_id == call.call_id)
                        .OrderBy(t => t.message_index)
                        .Select(t => $"{t.speaker}: {t.text}")
                        .ToListAsync(ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to fetch transcript for Call {CallId}", call.call_id);
                    return;
                }

                string transcriptText = string.Join("\n", fullTranscript);

                // ---------------------------
                // AI CALL (protected)
                // ---------------------------
                IAiInsightsService ai;
                CallInsightsResult result;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    ai = scope.ServiceProvider.GetRequiredService<IAiInsightsService>();

                    result = await ai.ExtractInsightsAsync(call.call_id, transcriptText, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "AI insight extraction failed for Call {CallId}", call.call_id);
                    return; // DO NOT crash polling loop
                }

                // ---------------------------
                // UPSERT CONFIRM DATA
                // ---------------------------
                try
                {
                    if (result.ConfirmData != null)
                    {
                        var existing = await _db.call_confirm_data
                            .FirstOrDefaultAsync(c => c.call_id == call.call_id, ct);

                        if (existing == null)
                        {
                            await _db.call_confirm_data.AddAsync(new CallConfirmData
                            {
                                call_id = call.call_id,
                                broker_name = result.ConfirmData.broker_name,
                                pickup_time = result.ConfirmData.PickupTime,
                                delivery_time = result.ConfirmData.DeliveryTime,
                                trip_mile = result.ConfirmData.TripMiles,
                                rate_per_mile = result.ConfirmData.RatePerMile,
                                final_rate = result.ConfirmData.FinalRate,
                                origin = result.ConfirmData.Origin,
                                destination = result.ConfirmData.Destination,
                                equipment_type = result.ConfirmData.equipment_type,
                                load_type = result.ConfirmData.load_type,
                                commodity = result.ConfirmData.load_type,
                                weight = result.ConfirmData.weight,
                                load_size = result.ConfirmData.load_size
                            }, ct);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(result.ConfirmData.broker_name))
                                existing.broker_name = result.ConfirmData.broker_name;

                            if (result.ConfirmData.PickupTime.HasValue)
                                existing.pickup_time = result.ConfirmData.PickupTime;

                            if (result.ConfirmData.DeliveryTime.HasValue)
                                existing.delivery_time = result.ConfirmData.DeliveryTime;

                            if (result.ConfirmData.TripMiles.HasValue)
                                existing.trip_mile = result.ConfirmData.TripMiles;

                            if (result.ConfirmData.RatePerMile.HasValue)
                                existing.rate_per_mile = result.ConfirmData.RatePerMile;

                            if (result.ConfirmData.FinalRate.HasValue)
                                existing.final_rate = result.ConfirmData.FinalRate;

                            if (!string.IsNullOrWhiteSpace(result.ConfirmData.Origin))
                                existing.origin = result.ConfirmData.Origin;

                            if (!string.IsNullOrWhiteSpace(result.ConfirmData.Destination))
                                existing.destination = result.ConfirmData.Destination;


                            if (!string.IsNullOrWhiteSpace(result.ConfirmData.equipment_type))
                                existing.equipment_type = result.ConfirmData.equipment_type;


                            if (!string.IsNullOrWhiteSpace(result.ConfirmData.load_type))
                                existing.load_type = result.ConfirmData.load_type;


                            if (!string.IsNullOrWhiteSpace(result.ConfirmData.commodity))
                                existing.commodity = result.ConfirmData.commodity;

                            if (result.ConfirmData.weight.HasValue)
                                existing.weight = result.ConfirmData.weight;

                            if (result.ConfirmData.load_size.HasValue)
                                existing.load_size = result.ConfirmData.load_size;
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to upsert ConfirmData for Call {CallId}", call.call_id);
                }

                // ---------------------------
                // UPSERT SENTIMENT
                // ---------------------------
                try
                {
                    if (result.Sentiment != null)
                    {
                        var existing = await _db.call_sentiment
                            .FirstOrDefaultAsync(s => s.call_id == call.call_id, ct);

                        if (existing == null)
                        {
                            await _db.call_sentiment.AddAsync(new CallSentiment
                            {
                                call_id = call.call_id,
                                sentiment = result.Sentiment
                            }, ct);
                        }
                        else
                        {
                            existing.sentiment = result.Sentiment;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to upsert Sentiment for Call {CallId}", call.call_id);
                }

                // ---------------------------
                // UPSERT BULLETS (Insert only new)
                // ---------------------------
                try
                {
                    if (result.SummaryBullets != null)
                    {
                        var existingBullets = await _db.call_summary_bullets
                            .Where(b => b.call_id == call.call_id)
                            .Select(b => b.text)
                            .ToListAsync(ct);

                        foreach (var bullet in result.SummaryBullets)
                        {
                            if (!existingBullets.Contains(bullet))
                            {
                                await _db.call_summary_bullets.AddAsync(new CallSummaryBullet
                                {
                                    call_id = call.call_id,
                                    text = bullet,
                                    timestamp = DateTime.UtcNow
                                }, ct);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to upsert SummaryBullets for Call {CallId}", call.call_id);
                }

                // ---------------------------
                // SAVE FINAL CHANGES
                // ---------------------------
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "DB SaveChanges failed for Call {CallId}", call.call_id);
                }

                // ---------------------------
                // BROADCAST TO SIGNALR
                // ---------------------------
                try
                {
                    await _hub.Clients.Group(call.call_id.ToString())
                        .SendAsync("ReceiveCallInsights", new
                        {
                            call_id = call.call_id,
                            confirmData = result.ConfirmData,
                            sentiment = result.Sentiment,
                            bullets = result.SummaryBullets
                        }, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "SignalR broadcast failed for Call {CallId}", call.call_id);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled error in ProcessInsightsAsync for Call {CallId}", call.call_id);
            }
        }
    }

    }
