using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICallTranscriptService
    {
        Task<ServiceResponse<List<CallTranscriptLineDto>>> GetTranscriptAsync(int callId, int companyId);
    }
}
