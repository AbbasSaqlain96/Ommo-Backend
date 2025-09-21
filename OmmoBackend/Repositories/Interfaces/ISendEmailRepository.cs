using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ISendEmailRepository
    {
        Task<int> InsertAsync(SendEmail row);
        Task MarkSentAsync(int id, DateTime sentAtUtc);
        Task MarkFailedAsync(int id, string errorMessage);
    }
}
