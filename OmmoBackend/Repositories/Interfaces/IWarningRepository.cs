
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using System.ComponentModel.Design;
using Twilio.Http;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IWarningRepository : IGenericRepository<Warning>
    {
        Task<bool> CreateWarningWithTransactionAsync(PerformanceEvents performanceEvents, WarningDocumentsDto warningDocumentsDto, List<int> violations, int companyId);
        Task<bool> CheckEventCompany(int eventId, int companyId);
        Task<WarningDetailsDto> FetchWarningDetailsAsync(int eventId, int companyId);
        Task<Warning> GetWarningByEventId(int eventId);
        Task<bool> WarningDocumentExist(int warningId);
    }
}
