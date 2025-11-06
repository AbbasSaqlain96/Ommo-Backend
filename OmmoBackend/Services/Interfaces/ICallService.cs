using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICallService
    {
        Task<OutboundCallResult> CallAsync(
            CompanyDialInfoDto company,
            LoadInfo load,
            ClientInfo client,
            Guid agentId,
            int companyId);

        Task<Guid?> FetchAgentIdAsync(int companyId);

        Task<ServiceResponse<List<CalledLoadDto>>> GetCalledLoadsAsync(int companyId);
    }
}
