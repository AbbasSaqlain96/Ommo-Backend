namespace OmmoBackend.Services.Interfaces
{
    public interface IAlertService
    {
        Task SendAsync(string reason, int companyId, string eventType, string message);
    }

    public class AlertService : IAlertService
    {
        private readonly IEmailService _emailService;

        public AlertService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendAsync(string reason, int companyId, string eventType, string message)
        {
            var subject =
                $"ALERT: {reason} — company_id: {companyId}";

            var body = $"""
                Event Type: {eventType}
                
                Company Id: {companyId}

                Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

                Description:
                {message}
                """;

            await _emailService.SendAsync(
                "info@ommo.ai",
                subject,
                body);
        }
    }
}
