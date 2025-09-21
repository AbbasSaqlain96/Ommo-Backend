using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface INotificationService
    {
        Task SaveNotificationAsync(Notification notification);
        Task<List<Notification>> GetRecentNotificationsAsync(int companyId);
        Task<List<Notification>> GetNotificationsAsync(int companyId);
    }
}
