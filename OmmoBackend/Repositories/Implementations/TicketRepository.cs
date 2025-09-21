using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.Net.Sockets;

namespace OmmoBackend.Repositories.Implementations
{
    public class TicketRepository : GenericRepository<UnitTicket>, ITicketRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ITicketDocRepository _ticketDocRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketRepository> _logger;
        public TicketRepository(AppDbContext dbContext, ITicketDocRepository ticketDocRepository, IConfiguration configuration, ILogger<TicketRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _ticketDocRepository = ticketDocRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> EventBelongsToCompany(int eventId, int companyId)
        {
            _logger.LogInformation("Checking if event {EventId} belongs to company {CompanyId}.", eventId, companyId);

            try
            {
                var exists = await _dbContext.performance_event
                            .AnyAsync(e => e.event_id == eventId && e.company_id == companyId);

                _logger.LogInformation("Event {EventId} belongs to company {CompanyId}: {Exists}", eventId, companyId, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking event ownership for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);
                throw;
            }
        }

        public async Task<TicketDetails> GetTicketDetailsAsync(int eventId, int companyId)
        {
            _logger.LogInformation("Fetching ticket details for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);

            try
            {
                var ticketData = await (from e in _dbContext.performance_event
                                        join t in _dbContext.unit_ticket on e.event_id equals t.event_id
                                        join d in _dbContext.driver on e.driver_id equals d.driver_id
                                        join td in _dbContext.ticket_doc on t.ticket_id equals td.ticket_id into tdGroup
                                        from td in tdGroup.DefaultIfEmpty()
                                        join tp in _dbContext.ticket_pictures on t.ticket_id equals tp.ticket_id into tpGroup
                                        from tp in tpGroup.DefaultIfEmpty()
                                        where e.event_id == eventId && e.company_id == companyId
                                        select new TicketDetails
                                        {
                                            TruckId = e.truck_id,
                                            DriverId = e.driver_id,
                                            DriverName = d.driver_name,
                                            TrailerId = e.trailer_id,
                                            Location = e.location,
                                            EventType = e.event_type.ToString(),
                                            Authority = e.authority.ToString(),
                                            EventDate = e.date,
                                            Description = e.description,
                                            LoadId = (int)e.load_id,
                                            EventFees = e.event_fees,
                                            FeesPaidBy = e.fees_paid_by.ToString(),
                                            CompanyFeeApplied = e.company_fee_applied,
                                            CompanyFeeAmount = e.company_fee_amount,
                                            CompanyFeeStatementDate = e.company_fee_statement_date,
                                            TicketStatus = t.status.ToString(),

                                            DocNumber = td != null ? td.doc_number : null,
                                            DocumentPath = td != null ? td.file_path : null,

                                            TicketDocumentURL = _dbContext.ticket_doc
                                                                 .Where(doc => doc.ticket_id == t.ticket_id)
                                                                 .Select(doc => doc.file_path)
                                                                 .FirstOrDefault(),

                                            ViolationIds = _dbContext.ticket_violation
                                            .Where(tv => tv.ticket_id == t.ticket_id)
                                            .Select(tv => tv.violation_id)
                                            .ToList(),

                                            TicketImages = _dbContext.ticket_pictures
                                            .Where(pic => pic.ticket_id == t.ticket_id)
                                            .Select(pic => pic.picture_url)
                                            .ToList()

                                        }).FirstOrDefaultAsync();

                if (ticketData == null)
                {
                    _logger.LogWarning("No ticket details found for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);
                }
                else
                {
                    _logger.LogInformation("Successfully fetched ticket details for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);
                }

                return ticketData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching ticket details for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);
                throw new ApplicationException("An unexpected error occurred while retrieving ticket details.", ex);
            }
        }

        public string SaveTicketDocument(int companyId, int driverId, string base64EncodedDocument)
        {
            _logger.LogInformation("Saving ticket document for CompanyId: {CompanyId}, DriverId: {DriverId}.", companyId, driverId);

            try
            {
                var directoryPath = Path.Combine("Documents", $"{companyId}", "Ticket_Doc", $"{driverId}");
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
                    Directory.CreateDirectory(directoryPath);
                }

                var fileName = $"TicketDOC_{driverId}_{Guid.NewGuid()}.pdf";
                var filePath = Path.Combine(directoryPath, fileName);

                var documentBytes = Convert.FromBase64String(base64EncodedDocument);
                File.WriteAllBytes(filePath, documentBytes);

                _logger.LogInformation("Ticket document saved at: {FilePath}", filePath);

                return filePath;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format for ticket document. CompanyId: {CompanyId}, DriverId: {DriverId}.", companyId, driverId);
                throw new ArgumentException("Invalid base64-encoded document format.", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "File write error while saving ticket document. CompanyId: {CompanyId}, DriverId: {DriverId}.", companyId, driverId);
                throw new ApplicationException("An error occurred while saving the ticket document.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while saving the ticket document. CompanyId: {CompanyId}, DriverId: {DriverId}.", companyId, driverId);
                throw;
            }
        }

        public async Task<int> CreateTicketAsync(int eventId, TicketInfoDto ticketInfo, string documentPath)
        {
            _logger.LogInformation("Creating ticket for EventId: {EventId} with status: {TicketStatus}", eventId, ticketInfo.Status);

            try
            {
                var ticket = new UnitTicket
                {
                    event_id = eventId,
                    status = ticketInfo.Status
                };

                await _dbContext.unit_ticket.AddAsync(ticket);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Ticket created successfully with TicketId: {TicketId} for EventId: {EventId}", ticket.ticket_id, eventId);

                return ticket.ticket_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating ticket for EventId: {EventId}", eventId);
                throw;
            }
        }

        public async Task<bool> CreateTicketWithTransactionAsync(
        PerformanceEvents performanceEvents,
        TicketInfoDto ticketInfo,
        TicketInfoDocumentDto documentInfo,
        List<int> violations,
        TicketImageDto imageDto,
        int companyId)
        {
            _logger.LogInformation("Starting transaction for creating ticket with Event details: {EventDetails}", performanceEvents);

            // Use the execution strategy provided by the database context
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    // Create Event
                    await _dbContext.performance_event.AddAsync(performanceEvents);
                    await _dbContext.SaveChangesAsync();
                    int eventId = performanceEvents.event_id;

                    _logger.LogInformation("Event created successfully with EventId: {EventId}", eventId);

                    // Create Ticket
                    var ticket = new UnitTicket
                    {
                        event_id = eventId,
                        status = ticketInfo.Status
                    };

                    await _dbContext.unit_ticket.AddAsync(ticket);
                    await _dbContext.SaveChangesAsync();
                    int ticketId = ticket.ticket_id;

                    _logger.LogInformation("Ticket created successfully with TicketId: {TicketId} for EventId: {EventId}", ticketId, eventId);

                    // Save Document
                    int lastTicketDocId = await _ticketDocRepository.GetLastTicketDocIdAsync();
                    int newTicketDocId = lastTicketDocId + 1;
                    var documentPath = await SaveDocumentAsync(companyId, performanceEvents.driver_id, ticketId, documentInfo.Document, newTicketDocId);

                    // Save Document in TicketDoc Table
                    var ticketDoc = new TicketDoc
                    {
                        doc_type_id = 26,
                        ticket_id = ticketId,
                        file_path = documentPath,
                        status = Enum.Parse<TicketDocStatus>("uploaded"),
                        doc_number = documentInfo.DocNumber
                    };

                    await _dbContext.ticket_doc.AddAsync(ticketDoc);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Ticket document saved successfully for TicketId: {TicketId} at Path: {DocumentPath}", ticketId, documentPath);

                    // Create Violations
                    var violationEntities = violations.Select(v => new ViolationTicket
                    {
                        ticket_id = ticketId,
                        violation_id = v,
                        violation_date = DateTime.UtcNow
                    }).ToList();

                    await _dbContext.ticket_violation.AddRangeAsync(violationEntities);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Added {ViolationCount} violations for TicketId: {TicketId}", violations.Count, ticketId);

                    // Save ticket images
                    var imagePaths = new List<string>();
                    if (imageDto != null && imageDto.TicketImages.Any())
                    {
                        foreach (var image in imageDto.TicketImages)
                        {
                            var imagePath = await SaveImageAsync(companyId, performanceEvents.driver_id, ticketId, image, newTicketDocId);
                            imagePaths.Add(imagePath);
                        }
                    }

                    // Save Ticket Images
                    var imageEntities = imagePaths.Select(path => new TicketPicture
                    {
                        ticket_id = ticketId,
                        picture_url = path
                    }).ToList();

                    await _dbContext.ticket_pictures.AddRangeAsync(imageEntities);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Ticket images saved successfully for TicketId: {TicketId}", ticketId);

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for EventId: {EventId}", eventId);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed. Rolling back changes for EventId: {EventId}", performanceEvents.event_id);
                    return false;
                }
            });
        }

        public async Task<(PerformanceEvents Event, UnitTicket Ticket)> GetTicketByEventIdAsync(int eventId)
        {
            var eventEntity = await _dbContext.performance_event
                .FirstOrDefaultAsync(e => e.event_id == eventId);

            if (eventEntity == null || eventEntity.event_type != EventType.ticket)
                return (null, null);

            var ticket = await _dbContext.unit_ticket
                .FirstOrDefaultAsync(t => t.event_id == eventId);

            return (eventEntity, ticket);
        }


        private async Task<string> SaveDocumentAsync(int companyId, int driverId, int ticketId, IFormFile document, int ticketDocId)
        {
            // Validate the base directory from configuration
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Ticket", companyId.ToString(), "Ticket_Doc", ticketId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Generate the file name
            var fileName = $"{Guid.NewGuid()}.pdf";
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
            string ticketDocumentUrl = $"{serverUrl}/Documents/Event/Ticket/{companyId}/Ticket_Doc/{ticketId}/{fileName}";

            return ticketDocumentUrl;
        }

        private async Task<string> SaveImageAsync(int companyId, int driverId, int ticketId, IFormFile image, int ticketDocId)
        {
            _logger.LogInformation("Saving ticket image for Company: {CompanyId}, Driver: {DriverId}", companyId, driverId);

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server ticket directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Ticket", companyId.ToString(), "Ticket_Image", ticketId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Generate the file name
            var fileExtension = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Get the server URL from configuration
            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogError("Server URL is not configured.");
                throw new InvalidOperationException("Server URL is not configured.");
            }

            // Construct the public URL dynamically
            string ticketImageUrl = $"{serverUrl}/Documents/Event/Ticket/{companyId}/Ticket_Image/{ticketId}/{fileName}";
            _logger.LogInformation("Generated public URL for ticket image: {TicketImageUrl}", ticketImageUrl);

            return ticketImageUrl;
        }

        public async Task<bool> DoesTicketBelongToCompanyAsync(int ticketId, int companyId)
        {
            return await _dbContext.unit_ticket
                .Where(t => t.ticket_id == ticketId)
                .Join(_dbContext.performance_event,
                    t => t.event_id,
                    e => e.event_id,
                    (t, e) => new { t, e })
                .AnyAsync(result => result.e.company_id == companyId);
        }

        public async Task<bool> TicketDocumentExist(int ticketId) 
        {
            return await _dbContext.ticket_doc
                .AnyAsync(d => d.ticket_id == ticketId);
        }
    }
}
