using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<List<Notification>> GetRecentNotificationsAsync(int companyId, int count);
        Task<List<Notification>> GetNotificationsAsync(int companyId);
    }
}
