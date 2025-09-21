using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IDotInspectionRepository : IGenericRepository<DocInspection>
    {
        Task<bool> CreateDotInspectionWithTransactionAsync(
            PerformanceEvents performanceEvents,
            DotInspectionDocInspectionInfoDto docInspectionDto,
            DocInspectionDocuments docInspectionDocumentsDto,
            List<int> violations,
            int companyId,
            DocInspectionStatus docInspectionStatus,
            int inspectionLevel,
            CitationStatus citationStatus);

        Task<bool> CheckEventCompany(int eventId, int companyId);

        Task<DotInspectionDetailsDto> FetchDotInspectionDetailsAsync(int eventId, int companyId);
    }
}
