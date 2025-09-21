using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using Twilio.Http;

namespace OmmoBackend.Repositories.Implementations
{
    public class DotInspectionRepository : GenericRepository<DocInspection>, IDotInspectionRepository
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DotInspectionRepository> _logger;
        public DotInspectionRepository(
            IConfiguration configuration,
            AppDbContext dbContext,
            ILogger<DotInspectionRepository> logger) : base(dbContext, logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CreateDotInspectionWithTransactionAsync(
            PerformanceEvents performanceEvents,
            DotInspectionDocInspectionInfoDto docInspectionDto,
            DocInspectionDocuments docInspectionDocumentsDto,
            List<int> violations,
            int companyId,
            DocInspectionStatus docInspectionStatus,
            int inspectionLevel,
            CitationStatus citationStatus)
        {
            // Use the execution strategy provided by the database context
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Starting dot inspection creation transaction for Company ID {CompanyId}.", companyId);

                    // Create Event
                    _logger.LogInformation("Creating dot inspection for Driver ID {DriverId}.", performanceEvents.driver_id);

                    await _dbContext.performance_event.AddAsync(performanceEvents);
                    await _dbContext.SaveChangesAsync();

                    int eventId = performanceEvents.event_id;

                    _logger.LogInformation("Performance event created successfully with Event ID {EventId}.", eventId);


                    // Create Doc Inspection
                    var docInspection = new DocInspection
                    {
                        status = docInspectionStatus,
                        inspection_level = inspectionLevel,
                        citation = citationStatus,
                        event_id = eventId
                    };

                    _logger.LogInformation("Creating doc inspection record for Event ID {EventId}.", eventId);

                    await _dbContext.doc_inspection.AddAsync(docInspection);
                    await _dbContext.SaveChangesAsync();

                    int docInspectionId = docInspection.doc_inspection_id;

                    _logger.LogInformation("Doc inspection record created successfully with ID {DocInspectionId}.", docInspectionId);

                    // Upload file
                    var documentPath = await SaveDocumentAsync(companyId, docInspectionId, docInspectionDocumentsDto.DocInspectionDoc);

                    var docInspectionDoc = new DocInspectionDocument
                    {
                        doc_type_id = 29,
                        doc_number = docInspectionDocumentsDto.DocNumber,
                        path = documentPath,
                        doc_inspection_id = docInspectionId,
                        status = "uploaded"
                    };

                    _dbContext.doc_inspection_documents.Add(docInspectionDoc);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Doc Inspection Document saved successfully");

                    // Insert violations
                    foreach (var violationId in violations)
                    {
                        _dbContext.doc_inspection_violation.Add(new DocInspectionViolation
                        {
                            doc_inspection_id = docInspectionId,
                            violation_id = violationId,
                            violation_date = performanceEvents.date
                        });
                    }

                    // Save all
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Violations record created successfully");

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for Dot Inspection ID {DocInspectionId }.", docInspectionId);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while creating dot inspection transaction for Company ID {CompanyId}. Rolling back transaction.", companyId);
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<DotInspectionDetailsDto> FetchDotInspectionDetailsAsync(int eventId, int companyId)
        {
            var eventData = await (from pe in _dbContext.performance_event
                                   join d in _dbContext.doc_inspection on pe.event_id equals d.event_id
                                   join drv in _dbContext.driver on pe.driver_id equals drv.driver_id
                                   where pe.event_id == eventId && pe.company_id == companyId
                                   select new DotInspectionDetailsDto
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
                                       Status = d.status.ToString(),
                                       InspectionLevel = (int)d.inspection_level,
                                       Citation = d.citation.ToString(),
                                   }).FirstOrDefaultAsync();

            if (eventData == null)
                return null;

            // Get inspection ID for document/violation joins
            var inspectionId = await _dbContext.doc_inspection
                .Where(d => d.event_id == eventId)
                .Select(d => d.doc_inspection_id)
                .FirstOrDefaultAsync();

            // Fetch documents
            eventData.Docs = await _dbContext.doc_inspection_documents
                .Where(doc => doc.doc_inspection_id == inspectionId)
                .Select(doc => new DocInspectionDocumentsDto
                {
                    DocNumber = doc.doc_number,
                    DocPath = doc.path
                }).ToListAsync();

            // Fetch violations
            eventData.ViolationIds = await _dbContext.doc_inspection_violation
                .Where(v => v.doc_inspection_id == inspectionId)
                .Select(v => v.violation_id)
                .ToListAsync();

            return eventData;
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

        private async Task<string> SaveDocumentAsync(int companyId, int docInspectionId, IFormFile document)
        {
            // Validate the base directory from configuration
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Dot_Inspection", companyId.ToString(), "Dot_Inspection_Document", docInspectionId.ToString());

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
            string docInspectionUrl = $"{serverUrl}/Documents/Event/Dot_Inspection/{companyId}/Dot_Inspection_Document/{docInspectionId}/{fileName}";
            return docInspectionUrl;
        }
    }
}
