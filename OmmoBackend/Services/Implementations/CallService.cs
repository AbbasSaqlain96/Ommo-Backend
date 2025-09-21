using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class CallService : ICallService
    {
        private readonly ICallRepository _callRepository;
        public CallService(ICallRepository callRepository)
        {
            _callRepository = callRepository;
        }

        public async Task<ServiceResponse<List<CalledLoadDto>>> GetCalledLoadsAsync(int companyId)
        {
            try
            {
                var loads = await _callRepository.GetCalledLoadsAsync(companyId);

                if (loads == null || !loads.Any())
                {
                    return ServiceResponse<List<CalledLoadDto>>.SuccessResponse(new List<CalledLoadDto>(), "No called loads found in last 24 hours.");
                }

                return ServiceResponse<List<CalledLoadDto>>.SuccessResponse(loads, "Called loads fetched successfully.");
            }
            catch (Exception)
            {
                return ServiceResponse<List<CalledLoadDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
