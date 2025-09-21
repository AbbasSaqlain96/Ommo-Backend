using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using System.Security.Claims;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAccidentRepository : IGenericRepository<Accident>
    {
        Task<Accident> GetAccidentByEventIdAsync(int eventId);

        Task<int> CreateAccidentAsync(Accident accident);
        
        Task<bool> CreateAccidentWithTransactionAsync(
            PerformanceEvents performanceEvents,
            AccidentInfoDto accidentInfo,
            AccidentDocumentDto accidentDocumentDto,
            AccidentImageDto accidentImageDto,
            List<Claims> accidentClaim,
            int companyId);

        Task<bool> PoliceReportDocumentExist(int accidentId);
        Task<bool> DriverReportDocumentExist(int accidentId);

        Task UpdateDocumentsAsync(
            int accidentId, 
            int companyId, 
            UpdateAccidentDocumentDto dto);

        Task SyncAccidentImagesAsync(
            int accidentId, 
            int eventId,
            int companyId, 
            List<IFormFile> newImages, 
            List<string> existingPaths);

        Task SyncClaimsAsync(
            int eventId, 
            List<Claims> updatedClaims);
    }
}
