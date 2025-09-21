using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class WarningRepository : GenericRepository<Warning>, IWarningRepository
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<WarningRepository> _logger;
        public WarningRepository(IConfiguration configuration, AppDbContext dbContext, ILogger<WarningRepository> logger) : base(dbContext, logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CheckEventCompany(int eventId, int companyId)
        {
            try
            {
                _logger.LogInformation("Checking if event {EventId} belongs to company {CompanyId}", eventId, companyId);

                var exists = await _dbContext.performance_event.AnyAsync(e => e.event_id == eventId && e.company_id == companyId);
                if (!exists)
                {
                    _logger.LogWarning("Event {EventId} does not belong to company {CompanyId}", eventId, companyId);
                }
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking if event {EventId} belongs to company {CompanyId}", eventId, companyId);
                throw;
            }
        }

        public async Task<bool> CreateWarningWithTransactionAsync(PerformanceEvents performanceEvents, WarningDocumentsDto warningDocumentsDto, List<int> violations, int companyId)
        {
            // Use the execution strategy provided by the database context
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Starting warning creation transaction for Company ID {CompanyId}.", companyId);

                    // Create Event
                    _logger.LogInformation("Creating warning.");

                    await _dbContext.performance_event.AddAsync(performanceEvents);
                    await _dbContext.SaveChangesAsync();

                    int eventId = performanceEvents.event_id;

                    _logger.LogInformation("Performance event created successfully with Event ID {EventId}.", eventId);

                    // Create Warning
                    var warning = new Warning
                    {
                        event_id = eventId
                    };

                    _logger.LogInformation("Creating warning record for Event ID {EventId}.", eventId);

                    await _dbContext.warning.AddAsync(warning);
                    await _dbContext.SaveChangesAsync();

                    int warningId = warning.warning_id;

                    if (warningDocumentsDto.DocPath != null)
                    {
                        // Upload file
                        var documentPath = await SaveDocumentAsync(companyId, warningId, warningDocumentsDto.DocPath);

                        // Create Warning Docs
                        var warningDocs = new WarningDocument
                        {
                            doc_type_id = 30,
                            doc_number = warningDocumentsDto.DocNumber,
                            path = documentPath,
                            warning_id = warningId,
                            status = "uploaded"
                        };

                        _dbContext.warning_documents.AddAsync(warningDocs);
                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation("Warning Document saved successfully");
                    }

                    // Insert violations
                    foreach (var violationId in violations)
                    {
                        _dbContext.warning_violation.Add(new WarningViolation
                        {
                            warning_id = warningId,
                            violation_id = violationId,
                            violation_date = performanceEvents.date
                        });
                    }

                    // Save all
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Violations record created successfully");

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for Warning ID {WarningId }.", warningId);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while creating warning transaction for Company ID {CompanyId}. Rolling back transaction.", companyId);
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<WarningDetailsDto> FetchWarningDetailsAsync(int eventId, int companyId)
        {
            var eventData = await (from pe in _dbContext.performance_event
                                   join w in _dbContext.warning on pe.event_id equals w.event_id
                                   join drv in _dbContext.driver on pe.driver_id equals drv.driver_id
                                   where pe.event_id == eventId && pe.company_id == companyId
                                   select new WarningDetailsDto
                                   {
                                       TruckId = pe.truck_id,
                                       DriverId = drv.driver_id,
                                       DriverName = drv.driver_name,
                                       TrailerId = pe.trailer_id,
                                       Location = pe.location,
                                       EventType = pe.event_type.ToString(),
                                       Authority = pe.authority.ToString(),
                                       EventDate = pe.date,
                                       Description = pe.description,
                                       LoadId = pe.load_id ?? 0,
                                       EventFee = pe.event_fees,
                                       FeesPaidBy = pe.fees_paid_by.ToString(),
                                       CompanyFeeApplied = pe.company_fee_applied,
                                       CompanyFeeAmount = pe.company_fee_amount,
                                       CompanyFeeStatementDate = pe.company_fee_statement_date ?? DateTime.MinValue,
                                   }).FirstOrDefaultAsync();

            if (eventData == null)
                return null;

            // Get warning ID for document/violation joins
            var warningId = await _dbContext.warning
                .Where(d => d.event_id == eventId)
                .Select(d => d.warning_id)
                .FirstOrDefaultAsync();

            // Fetch documents
            eventData.Docs = await _dbContext.warning_documents
                .Where(doc => doc.warning_id == warningId)
                .Select(doc => new WarningDocumentsDetailDto
                {
                    DocNumber = doc.doc_number,
                    DocPath = doc.path
                }).ToListAsync();

            // Fetch violations
            eventData.ViolationIds = await _dbContext.warning_violation
                .Where(v => v.warning_id == warningId)
                .Select(v => v.violation_id)
                .ToListAsync();

            return eventData;
        }

        private async Task<string> SaveDocumentAsync(int companyId, int warningId, IFormFile document)
        {
            // Validate the base directory from configuration
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Warning", companyId.ToString(), "Warning_Document", warningId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Generate the file name
            var fileName = $"{Guid.NewGuid()}_{document.FileName}";
            var filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await document.CopyToAsync(stream);
                }
                _logger.LogInformation("Document saved successfully at: {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server at: {FilePath}", filePath);
                throw new InvalidOperationException("Error writing document to the server: " + ex.Message);
            }

            // Get the server URL from configuration
            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
            if (string.IsNullOrWhiteSpace(serverUrl))
                throw new InvalidOperationException("Server URL is not configured.");

            // Construct the public URL in the required format
            string warningUrl = $"{serverUrl}/Documents/Event/Warning/{companyId}/Warning_Document/{warningId}/{fileName}";
            return warningUrl;
        }

        public async Task<Warning> GetWarningByEventId(int eventId) 
        {
            return await _dbContext.warning.Where(x => x.event_id == eventId).FirstOrDefaultAsync();
        }

        public async Task<bool> WarningDocumentExist(int warningId)
        {
            return await _dbContext.warning_documents
                .AnyAsync(d => d.warning_id == warningId);
        }
    }
}
