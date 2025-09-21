using Microsoft.AspNetCore.SignalR;
using OmmoBackend.Data;
using OmmoBackend.Hubs;
using OmmoBackend.Models;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Middlewares
{
    public class NotificationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public NotificationMiddleware(RequestDelegate next, IHubContext<NotificationHub> hubContext, ILogger<NotificationMiddleware> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _hubContext = hubContext;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context); // Execute the next middleware in the pipeline

            if (context.Response.StatusCode == 200) // Send notifications only on successful responses
            {
                var endpoint = context.GetEndpoint();
                if (endpoint != null)
                {
                    var metadata = endpoint.Metadata.GetMetadata<NotificationMetadata>();
                    if (metadata != null)
                    {
                        var module = metadata.Module;
                        var component = metadata.Component;
                        var accessLevel = metadata.AccessLevel;

                        _logger.LogInformation("Processing notification for Module: {Module}, Component: {Component}, AccessLevel: {AccessLevel}",
                            module, component, accessLevel);

                        var companyId = context.User.FindFirst("Company_ID")?.Value;

                        if (!string.IsNullOrEmpty(companyId))
                        {
                            _logger.LogInformation("Storing Notification in DB");

                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                                var notification = new Notification
                                {
                                    company_id = int.Parse(companyId),
                                    module = metadata.Module,
                                    component = metadata.Component,
                                    access_level = metadata.AccessLevel,
                                    message = $"New activity in {metadata.Module} - {metadata.Component}",
                                    created_at = DateTime.UtcNow
                                };

                                await notificationService.SaveNotificationAsync(notification);
                            }

                            _logger.LogInformation("Notification stored in DB");

                            _logger.LogInformation("Sending notification to company ID: {CompanyId}", companyId);

                            await _hubContext.Clients.Group(companyId)
                                .SendAsync("ReceiveNotification", new
                                {
                                    Module = module,
                                    Component = component,
                                    AccessLevel = accessLevel,
                                    Message = $"New activity in {module} - {component}"
                                });

                            _logger.LogInformation("Notification sent successfully for Module: {Module}, Component: {Component}", module, component);
                        }
                        else
                        {
                            _logger.LogWarning("Company ID not found in user claims.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No NotificationMetadata found for the endpoint.");
                    }
                }
                else
                {
                    _logger.LogWarning("Endpoint not found in HttpContext.");
                }
            }
            else
            {
                _logger.LogDebug("Skipping notification. Response status code: {StatusCode}", context.Response.StatusCode);
            }
        }
    }
}
