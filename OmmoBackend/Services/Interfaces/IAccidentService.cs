using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAccidentService
    {
        Task<ServiceResponse<AccidentCreationResult>> CreateAccidentAsync(
            int companyId, CreateAccidentRequest createAccidentRequest);

        Task<ServiceResponse<AccidentUpdateResult>> UpdateAccidentAsync
            (UpdateAccidentRequest request, int companyId);
    }
}
