using System.Text.Json;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICallService
    {
        Task<OutboundCallResult> CallAsync(
            CompanyDialInfoDto company,
            LoadInfo load,
            ClientInfo client,
            Guid agentId,
            int companyId,
            Guid call_id,
            int userid);

        Task<Guid?> FetchAgentIdAsync(int companyId);

        Task<ServiceResponse<List<CalledLoadDto>>> GetCalledLoadsAsync(int companyId);
        Task<Guid> LogCallAsync(Call call, CancellationToken ct = default);

        Task UpdateTwilioCallStatusAsync(TwilioStatusCallbackRequest request);

        Task TakeoverCallAsync(Guid callId, int companyid, int userid, string takeovernumber);
        Task<ServiceResponse<List<CallResponse>>> GetCallsAsync(int companyId, string? statusFilter);

        Task UpdateCallAfterDialAsync(
        Guid callId,
        OutboundCallResult callResult);

    }
}
