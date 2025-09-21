using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IDriverDocumentService
    {
        Task<ServiceResponse<List<DriverDocumentDto>>> GetDriverDocumentsAsync(int driverId, int companyId);
    }
}
