using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ITranscriptRepository
    {
        Task<List<CallTranscript>> GetByCallIdAsync(int callId);
    }
}
