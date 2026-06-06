using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICallRepository : IGenericRepository<Call>
    {
        Task<List<CalledLoadDto>> GetCalledLoadsAsync(int companyId);


        Task<Guid> InsertAsync(Call call, CancellationToken ct = default);

        Task UpdateStatusByTwilioSidAsync(
        string twilioSid,
        string statusOfCall,
        string? callresult,
        CancellationToken ct = default);

        Task<Guid?> GetCallIdByTwilioSidAsync(string callSid);

        Task<List<string>> GetDistinctCallStatusesAsync();
        //Task<List<Call>> GetCallsAsync(int companyId, string? statusFilter);
        Task<List<CallResponse>> GetCallsAsync(int companyId, string? statusFilter);
        Task UpdateAfterDialAsync(
        Guid callId,
        OutboundCallResult callResult);

        Task<CallTakeoverInfo?> GetCallForTakeoverAsync(Guid callId);
    }
}
