using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class DriverDocumentRepository : GenericRepository<DriverDoc>, IDriverDocumentRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DriverDocumentRepository> _logger;

        public DriverDocumentRepository(AppDbContext dbContext, ILogger<DriverDocumentRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CheckDriverCompany(int driverId, int companyId)
        {
            try
            {
                _logger.LogInformation("Checking if driver ID {DriverId} belongs to company ID {CompanyId}", driverId, companyId);

                var exists = await _dbContext.driver.AnyAsync(d => d.driver_id == driverId && d.company_id == companyId);

                _logger.LogInformation("Driver ID {DriverId} {Exists} in company ID {CompanyId}", driverId, exists ? "exists" : "does not exist", companyId);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking driver-company association for driver ID {DriverId} and company ID {CompanyId}", driverId, companyId);
                throw;
            }
        }

        public async Task<List<DriverDocumentDto>> GetDriverDocumentsAsync(int driverId)
        {
            try
            {
                _logger.LogInformation("Fetching documents for driver ID {DriverId}", driverId);

                var documents = await _dbContext.driver_doc
                    .Where(dd => dd.driver_id == driverId)
                    .Join(_dbContext.document_type,
                          dd => dd.doc_type_id,
                          dt => dt.doc_type_id,
                          (dd, dt) => new DriverDocumentDto
                          {
                              DocName = dt.doc_name,
                              URL = dd.file_path,
                              Status = dd.status.ToString(),
                              EndDate = dd.end_date.ToString("yyyy-MM-dd")
                          })
                    .ToListAsync();

                _logger.LogInformation("Fetched {DocumentCount} documents for driver ID {DriverId}", documents.Count, driverId);
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching documents for driver ID {DriverId}", driverId);
                throw new Exception("An error occurred while retrieving driver documents.");
            }
        }
    }
}
