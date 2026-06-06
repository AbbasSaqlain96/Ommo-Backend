using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ISupportRequestRepository
    {
        Task AddAsync(SupportRequest supportRequest);
    }
}
