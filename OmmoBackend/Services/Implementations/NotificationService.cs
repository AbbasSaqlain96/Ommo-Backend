using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;
        public NotificationService(INotificationRepository notificationRepository, ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task SaveNotificationAsync(Notification notification)
        {
            try
            {
                _logger.LogInformation("Saving notification for Company ID: {CompanyId}", notification.company_id);
                await _notificationRepository.AddAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification for Company ID: {CompanyId}", notification.company_id);
                throw;
            }
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching recent notifications for Company ID: {CompanyId}", companyId);
                return await _notificationRepository.GetRecentNotificationsAsync(companyId, 10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent notifications for Company ID: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<List<Notification>> GetNotificationsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching all notifications for Company ID: {CompanyId}", companyId);
                return await _notificationRepository.GetNotificationsAsync(companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications for Company ID: {CompanyId}", companyId);
                throw;
            }
        }
    }
}
