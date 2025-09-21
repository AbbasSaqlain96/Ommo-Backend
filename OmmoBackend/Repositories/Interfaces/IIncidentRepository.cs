
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IIncidentRepository
    {
        Task<bool> CheckEventCompany(int eventId, int companyId);
        Task<IncidentDetailsDto> FetchIncidentDetailsAsync(int eventId, int companyId);
        Task<int> CreateIncidentAsync(Incident incident);
        Task<bool> CreateIncidentWithTransactionAsync(
            PerformanceEvents performanceEvents,
            IncidentInfoDto ticketInfo,
            IncidentEventInfoDto eventInfoDto,
            List<IFormFile> images,
            List<IncidentDocumentDto> docs,
            List<Claims> incidentClaims,
            int companyId);

        Task UpdateIncidentTypesAsync(int eventId, List<int?> newTypes);
        Task UpdateEquipmentDamagesAsync(int eventId, List<int?> newDamages);

        Task<Incident> GetIncidentByEventId(int eventId);
    }
}
