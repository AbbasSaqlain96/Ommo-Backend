using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.ComponentModel.Design;
using System.Security.Claims;

namespace OmmoBackend.Repositories.Implementations
{
    public class AccidentRepository : GenericRepository<Accident>, IAccidentRepository
    {
        private readonly IAccidentDocRepository _accidentDocRepository;
        private readonly IAccidentPicturesRepository _accidentPicturesRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly IFileStorageService _fileService;
        private readonly ILogger<AccidentRepository> _logger;

        public AccidentRepository(
            AppDbContext dbContext,
            IAccidentDocRepository accidentDocRepository,
            IConfiguration configuration,
            IAccidentPicturesRepository accidentPicturesRepository,
            IClaimRepository claimRepository,
            IFileStorageService fileService,
            ILogger<AccidentRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _accidentDocRepository = accidentDocRepository;
            _configuration = configuration;
            _accidentPicturesRepository = accidentPicturesRepository;
            _claimRepository = claimRepository;
            _fileService = fileService;
            _logger = logger;
        }


        public async Task<Accident> GetAccidentByEventIdAsync(int eventId)
        {
            var accident = _dbContext.accident
                .Where(x => x.event_id == eventId)
                .FirstOrDefault();

            return accident != null ? accident : null;
        }

        public async Task<int> CreateAccidentAsync(Accident accident)
        {
            try
            {
                _logger.LogInformation("Creating an accident record for Event ID {EventId}.", accident.event_id);
                _dbContext.accident.Add(accident);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Accident record created successfully with ID {AccidentId}.", accident.accident_id);
                return accident.accident_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating an accident record.");
                throw;
            }
        }

        public async Task<bool> CreateAccidentWithTransactionAsync(
            PerformanceEvents performanceEvents,
            AccidentInfoDto accidentInfo,
            AccidentDocumentDto accidentDocumentDto,
            AccidentImageDto accidentImageDto,
            List<Claims> accidentClaim,
            int companyId)
        {
            // Use the execution strategy provided by the database context
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Starting accident creation transaction for Company ID {CompanyId}.", companyId);

                    // Create Event
                    _logger.LogInformation("Creating performance event for Driver ID {DriverId}.", performanceEvents.driver_id);

                    await _dbContext.performance_event.AddAsync(performanceEvents);
                    await _dbContext.SaveChangesAsync();

                    int eventId = performanceEvents.event_id;

                    _logger.LogInformation("Performance event created successfully with Event ID {EventId}.", eventId);

                    // Create Accident
                    var accident = new Accident
                    {
                        driver_fault = accidentInfo.DriverFault,
                        alcohol_test = accidentInfo.AlcoholTest,
                        drug_test_date_time = accidentInfo.DrugTestDateTime,
                        alcohol_test_date_time = accidentInfo.AlcoholTestDateTime,
                        has_casuality = accidentInfo.HasCasualty == true,
                        driver_drug_test = accidentInfo.DriverDrugTest == true,
                        ticket_id = accidentInfo.TicketId,
                        event_id = eventId
                    };

                    _logger.LogInformation("Creating accident record for Event ID {EventId}.", eventId);

                    await _dbContext.accident.AddAsync(accident);
                    await _dbContext.SaveChangesAsync();

                    int accidentId = accident.accident_id;

                    _logger.LogInformation("Accident record created successfully with ID {AccidentId}.", accidentId);

                    // Save Documents
                    int lastAccidentDocId = await _accidentDocRepository.GetLastAccidentDocIdAsync();
                    int newAccidentDocId = lastAccidentDocId + 1;

                    var accidentDocs = new List<AccidentDoc>();

                    if (accidentDocumentDto.DriverReportFile != null)
                    {
                        _logger.LogInformation("Saving driver report document for Accident ID {AccidentId}.", accidentId);
                        var driverReportPath = await SaveDriverReportDocumentAsync(companyId, performanceEvents.driver_id, accidentDocumentDto.DriverReportFile, accidentId, newAccidentDocId);

                        accidentDocs.Add(new AccidentDoc
                        {
                            doc_type_id = 24, // Driver Report
                            accident_id = accidentId,
                            file_path = driverReportPath,
                            doc_number = accidentDocumentDto.DriverReportNumber,
                            status = Enum.Parse<AccidentDocStatus>("uploaded")
                        });
                    }

                    if (accidentDocumentDto.PoliceReportFile != null)
                    {
                        _logger.LogInformation("Saving police report document for Accident ID {AccidentId}.", accidentId);
                        var policeReportPath = await SavePoliceReportDocumentAsync(companyId, performanceEvents.driver_id, accidentDocumentDto.PoliceReportFile, accidentId, newAccidentDocId);

                        accidentDocs.Add(new AccidentDoc
                        {
                            doc_type_id = 25, // Police Report
                            accident_id = accidentId,
                            file_path = policeReportPath,
                            doc_number = accidentDocumentDto.PoliceReportNumber,
                            status = Enum.Parse<AccidentDocStatus>("uploaded")
                        });
                    }

                    // Add multiple documents in a single operation
                    if (accidentDocs.Any())
                    {
                        _logger.LogInformation("Adding {Count} accident documents for Accident ID {AccidentId}.", accidentDocs.Count, accidentId);
                        await _accidentDocRepository.AddMultipleAccidentDocsAsync(accidentDocs);
                    }

                    // Save Accident Images
                    if (accidentImageDto.AccidentImages != null && accidentImageDto.AccidentImages.Any())
                    {
                        _logger.LogInformation("Saving {Count} accident images for Accident ID {AccidentId}.", accidentImageDto.AccidentImages.Count, accidentId);
                        foreach (var picture in accidentImageDto.AccidentImages)
                        {
                            string pictureUrl = await SaveAccidentImageAsync(companyId, performanceEvents.driver_id, accidentId, picture);

                            var accidentPicture = new AccidentPicture
                            {
                                accident_id = accidentId,
                                picture_url = pictureUrl
                            };

                            await _accidentPicturesRepository.AddAsync(accidentPicture);
                        }
                    }

                    // Create Claims
                    foreach (var claim in accidentClaim)
                    {
                        _logger.LogInformation("Creating claim record for Accident ID {AccidentId}.", accidentId);
                        var claimRecord = new Claims
                        {
                            event_id = eventId,
                            claim_type = claim.claim_type,
                            status = claim.status,
                            claim_amount = claim.claim_amount,
                            claim_description = claim.claim_description
                        };
                        await _claimRepository.CreateClaimAsync(claimRecord);
                    }

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for Accident ID {AccidentId}.", accidentId);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while creating an accident transaction for Company ID {CompanyId}. Rolling back transaction.", companyId);
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        private async Task<string> SaveDriverReportDocumentAsync(int companyId, int driverId, IFormFile DrugTestReport, int accidentId, int accidentDocId)
        {
            _logger.LogInformation("Starting SaveDriverReportDocumentAsync for CompanyId: {CompanyId}, DriverId: {DriverId}, AccidentId: {AccidentId}, AccidentDocId: {AccidentDocId}", companyId, driverId, accidentId, accidentDocId);

            // Validate the base directory from configuration
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Accident", companyId.ToString(), "Accident_Doc", accidentId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Created directory: {FolderPath}", folderPath);
                Directory.CreateDirectory(folderPath);
            }

            // Generate the file name
            var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
            var filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await DrugTestReport.CopyToAsync(stream);
                }
                _logger.LogInformation("File saved successfully at {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server at {FilePath}", filePath);
                throw new InvalidOperationException("Error writing document to the server: " + ex.Message);
            }

            // Get the server URL from configuration
            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogError("Server URL is not configured.");
                throw new InvalidOperationException("Server URL is not configured.");
            }

            // Construct the public URL in the required format
            string accidentDocumentUrl = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}";
            _logger.LogInformation("Generated document URL: {AccidentDocumentUrl}", accidentDocumentUrl);

            return accidentDocumentUrl;
        }
        private async Task<string> SavePoliceReportDocumentAsync(int companyId, int driverId, IFormFile PoliceReport, int accidentId, int accidentDocId)
        {
            _logger.LogInformation("Starting SavePoliceReportDocumentAsync for CompanyId: {CompanyId}, DriverId: {DriverId}, AccidentId: {AccidentId}, AccidentDocId: {AccidentDocId}", companyId, driverId, accidentId, accidentDocId);

            // Validate the base directory from configuration
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Accident", companyId.ToString(), "Accident_Doc", accidentId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Created directory: {FolderPath}", folderPath);
                Directory.CreateDirectory(folderPath);
            }

            // Generate the file name
            var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
            var filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PoliceReport.CopyToAsync(stream);
                }
                _logger.LogInformation("File saved successfully at {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server at {FilePath}", filePath);
                throw new InvalidOperationException("Error writing document to the server: " + ex.Message);
            }

            // Get the server URL from configuration
            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogError("Server URL is not configured.");
                throw new InvalidOperationException("Server URL is not configured.");
            }

            // Construct the public URL in the required format
            string accidentDocumentUrl = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}";
            _logger.LogInformation("Generated document URL: {AccidentDocumentUrl}", accidentDocumentUrl);

            return accidentDocumentUrl;
        }
        private async Task<string> SaveAccidentImageAsync(int companyId, int driverId, int accidentId, IFormFile picture)
        {
            _logger.LogInformation("Starting SaveAccidentImageAsync for CompanyId: {CompanyId}, DriverId: {DriverId}, AccidentId: {AccidentId}", companyId, driverId, accidentId);

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server accident directory is not configured.");
                throw new InvalidOperationException("Server accident directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Accident", companyId.ToString(), "Accident_Pictures", accidentId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Creating directory: {FolderPath}", folderPath);
                Directory.CreateDirectory(folderPath);
            }

            // Generate the file name
            string fileExtension = Path.GetExtension(picture.FileName);  // Get the extension (e.g., ".png")
            string fileName = $"{accidentId}_{Guid.NewGuid()}{fileExtension}"; // Generate a unique filename
            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                _logger.LogInformation("Saving accident image to {FilePath}", filePath);
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await picture.CopyToAsync(stream);
                }
                _logger.LogInformation("Accident image saved successfully: {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server: {Message}", ex.Message);
                throw new InvalidOperationException("Error writing document to the server: " + ex.Message);
            }

            // Get the server URL from configuration
            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogError("Server URL is not configured.");
                throw new InvalidOperationException("Server URL is not configured.");
            }

            // Construct the public URL dynamically
            string accidentImageUrl = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Pictures/{accidentId}/{fileName}";
            _logger.LogInformation("Generated accident image URL: {AccidentImageUrl}", accidentImageUrl);

            return accidentImageUrl;
        }


        public async Task<bool> PoliceReportDocumentExist(int accidentId)
        {
            return await _dbContext.accident_doc
                .AnyAsync(d => d.accident_id == accidentId);
        }

        public async Task<bool> DriverReportDocumentExist(int accidentId)
        {
            return await _dbContext.accident_doc
                .AnyAsync(d => d.accident_id == accidentId);
        }

        public async Task UpdateDocumentsAsync(int accidentId, int companyId, UpdateAccidentDocumentDto dto)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath, "Event", "Accident", companyId.ToString(), "Accident_Doc", accidentId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Remove existing document (if any)
            var existingDoc = await _dbContext.accident_doc
                .Where(d => d.accident_id == accidentId).ToListAsync();

            if (!existingDoc.Any())
            {
                if (dto.PoliceReportFile != null && dto.PoliceReportNumber != null)
                {
                    // Save new file
                    string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                    var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
                    var fullFilePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(fullFilePath, FileMode.Create))
                    {
                        await dto.PoliceReportFile.CopyToAsync(stream);
                    }

                    _dbContext.accident_doc.Add(new AccidentDoc
                    {
                        accident_id = accidentId,
                        doc_type_id = 25,
                        doc_number = dto.PoliceReportNumber,
                        file_path = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}",
                        status = AccidentDocStatus.uploaded,
                        update_date = DateTime.UtcNow
                    });
                }
            }
            else
            {
                if (dto.PoliceReportFile != null || dto.PoliceReportNumber != null)
                {
                    var policeDoc = existingDoc.FirstOrDefault(d => d.doc_type_id == 25);

                    if (dto.PoliceReportFile != null)
                    {
                        if (policeDoc != null) // check policeDoc value
                        {
                            // remove doc
                            var oldFileName = Path.GetFileName(policeDoc.file_path);
                            var oldFilePath = Path.Combine(folderPath, oldFileName);
                            if (File.Exists(oldFilePath))
                                File.Delete(oldFilePath);

                            _dbContext.accident_doc.Remove(policeDoc);

                            // Save new file
                            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                            var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
                            var fullFilePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(fullFilePath, FileMode.Create))
                            {
                                await dto.PoliceReportFile.CopyToAsync(stream);
                            }

                            // Save record in DB
                            _dbContext.accident_doc.Add(new AccidentDoc
                            {
                                accident_id = accidentId,
                                doc_type_id = 25,
                                doc_number = policeDoc.doc_number,
                                file_path = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}",
                                status = AccidentDocStatus.uploaded,
                                update_date = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            // Save new file
                            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                            var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
                            var fullFilePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(fullFilePath, FileMode.Create))
                            {
                                await dto.PoliceReportFile.CopyToAsync(stream);
                            }

                            _dbContext.accident_doc.Add(new AccidentDoc
                            {
                                accident_id = accidentId,
                                doc_type_id = 25,
                                doc_number = policeDoc.doc_number,
                                file_path = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}",
                                status = AccidentDocStatus.uploaded,
                                update_date = DateTime.UtcNow
                            });
                        }
                    }
                    else if (policeDoc != null)
                    {
                        // remove doc
                        var oldFileName = Path.GetFileName(policeDoc.file_path);
                        var oldFilePath = Path.Combine(folderPath, oldFileName);
                        if (File.Exists(oldFilePath))
                            File.Delete(oldFilePath);

                        _dbContext.accident_doc.Remove(policeDoc);
                    }
                }
            }

            if (!existingDoc.Any())
            {
                if (dto.DriverReportFile != null && dto.DriverReportNumber != null)
                {
                    // Save new file
                    string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                    var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
                    var fullFilePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(fullFilePath, FileMode.Create))
                    {
                        await dto.DriverReportFile.CopyToAsync(stream);
                    }

                    _dbContext.accident_doc.Add(new AccidentDoc
                    {
                        accident_id = accidentId,
                        doc_type_id = 25,
                        doc_number = dto.DriverReportNumber,
                        file_path = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}",
                        status = AccidentDocStatus.uploaded,
                        update_date = DateTime.UtcNow
                    });
                }
            }
            else
            {
                if (dto.DriverReportFile != null || dto.DriverReportNumber != null)
                {
                    var driverDoc = existingDoc.FirstOrDefault(d => d.doc_type_id == 24);
                    
                    if (dto.DriverReportFile != null)
                    {
                        if (driverDoc != null)
                        {
                            // remove doc
                            var oldFileName = Path.GetFileName(driverDoc.file_path);
                            var oldFilePath = Path.Combine(folderPath, oldFileName);
                            if (File.Exists(oldFilePath))
                                File.Delete(oldFilePath);

                            _dbContext.accident_doc.Remove(driverDoc);

                            // Save new file
                            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                            var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
                            var fullFilePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(fullFilePath, FileMode.Create))
                            {
                                await dto.DriverReportFile.CopyToAsync(stream);
                            }

                            // Save record in DB
                            _dbContext.accident_doc.Add(new AccidentDoc
                            {
                                accident_id = accidentId,
                                doc_type_id = 24,
                                doc_number = driverDoc.doc_number,
                                file_path = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}",
                                status = AccidentDocStatus.uploaded,
                                update_date = DateTime.UtcNow
                            });

                        }
                        else
                        {
                            // Save new file
                            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                            var fileName = $"{Guid.NewGuid()}_{accidentId}.pdf";
                            var fullFilePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(fullFilePath, FileMode.Create))
                            {
                                await dto.DriverReportFile.CopyToAsync(stream);
                            }

                            _dbContext.accident_doc.Add(new AccidentDoc
                            {
                                accident_id = accidentId,
                                doc_type_id = 24,
                                doc_number = driverDoc.doc_number,
                                file_path = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Doc/{accidentId}/{fileName}",
                                status = AccidentDocStatus.uploaded,
                                update_date = DateTime.UtcNow
                            });
                        }
                    }
                    else if (driverDoc != null)
                    {
                        // remove doc
                        var oldFileName = Path.GetFileName(driverDoc.file_path);
                        var oldFilePath = Path.Combine(folderPath, oldFileName);
                        if (File.Exists(oldFilePath))
                            File.Delete(oldFilePath);

                        _dbContext.accident_doc.Remove(driverDoc);
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task SyncAccidentImagesAsync(int accidentId, int eventId, int companyId, List<IFormFile> newImages, List<string> existingImagePaths)
        {
            var accidentExists = await (from a in _dbContext.accident
                                        join e in _dbContext.performance_event
                                        on a.event_id equals e.event_id
                                        where a.accident_id == accidentId && e.company_id == companyId
                                        select a.accident_id).AnyAsync();
            if (!accidentExists)
            {
                _logger.LogWarning("Attempted to sync accident images for non-existent AccidentId: {AccidentId}, CompanyId: {CompanyId}", accidentId, companyId);
                throw new ArgumentException("Accident not found for the provided ID and company.");
            }

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");
            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server incident directory is not configured.");
                throw new InvalidOperationException("Server incident directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Accident", companyId.ToString(), "Accident_Pictures", accidentId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // 1. Get all accident IDs for this event
            var accidentIds = await _dbContext.accident
                .Where(i => i.event_id == eventId)
                .Select(i => i.accident_id)
                .ToListAsync();

            if (!accidentIds.Any())
                return;

            // 2. Get existing images from DB
            var existingImages = await _dbContext.accident_pictures
                .Where(p => accidentIds.Contains(p.accident_id))
                .ToListAsync();

            // 3. Prepare filenames for comparison
            var newFileNames = newImages.Select(f => f.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 4. Delete removed images
            var toDelete = existingImages
                .Where(e => !newFileNames.Contains(Path.GetFileName(e.picture_url)))
                .ToList();

            foreach (var image in toDelete)
            {
                // Delete physical file
                var filePath = Path.Combine(folderPath, Path.GetFileName(image.picture_url));
                if (File.Exists(filePath))
                    File.Delete(filePath);

                _dbContext.accident_pictures.Remove(image);
            }

            // 5. Add new images
            foreach (var file in newImages)
            {
                if (existingImages.Any(e => Path.GetFileName(e.picture_url).Equals(file.FileName, StringComparison.OrdinalIgnoreCase)))
                    continue; // already exists

                string fileExtension = Path.GetExtension(file.FileName);
                var uniqueName = $"{accidentId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(folderPath, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get the server URL from configuration
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    _logger.LogError("Server URL is not configured.");
                    throw new InvalidOperationException("Server URL is not configured.");
                }

                // Save one image per accident
                foreach (var incidentId in accidentIds)
                {
                    _dbContext.accident_pictures.Add(new AccidentPicture
                    {
                        accident_id = accidentId,
                        picture_url = $"{serverUrl}/Documents/Event/Accident/{companyId}/Accident_Pictures/{accidentId}/{uniqueName}",
                        uploaded_at = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task SyncClaimsAsync(int eventId, List<Claims> updatedClaims)
        {
            var existingClaims = await _dbContext.claim.Where(c => c.event_id == eventId).ToListAsync();
            var incomingIds = updatedClaims
                .Select(c => c.claim_id)
                .ToList();
            //.Where(c => c.claim_id).Select(c => c.ClaimId.Value).ToList();

            // Delete missing claims
            var toDelete = existingClaims.Where(c => !incomingIds.Contains(c.claim_id)).ToList();
            _dbContext.claim.RemoveRange(toDelete);

            // Update or insert
            foreach (var claim in updatedClaims)
            {
                if (claim.claim_id != 0)
                {
                    var existing = existingClaims.FirstOrDefault(c => c.claim_id == claim.claim_id);
                    if (existing != null)
                    {
                        existing.claim_type = claim.claim_type;
                        existing.claim_description = claim.claim_description;
                        existing.claim_amount = claim.claim_amount;
                        existing.status = claim.status;
                        existing.updated_at = DateTime.UtcNow;
                    }
                }
                else
                {
                    _dbContext.claim.Add(new Claims
                    {
                        event_id = eventId,
                        claim_type = claim.claim_type,
                        claim_description = claim.claim_description,
                        claim_amount = claim.claim_amount,
                        status = claim.status,
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
