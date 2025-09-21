using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.Data;
using System.Security.Claims;

namespace OmmoBackend.Repositories.Implementations
{
    public class IncidentRepository : GenericRepository<Incident>, IIncidentRepository
    {
        private readonly IIncidentPicturesRepository _incidentPicturesRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<IncidentRepository> _logger;
        public IncidentRepository(AppDbContext dbContext, IConfiguration configuration, IIncidentPicturesRepository incidentPicturesRepository, IClaimRepository claimRepository, ILogger<IncidentRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _incidentPicturesRepository = incidentPicturesRepository;
            _claimRepository = claimRepository;
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

        public async Task<IncidentDetailsDto> FetchIncidentDetailsAsync(int eventId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching incident details for event {EventId} and company {CompanyId}", eventId, companyId);

                var eventData = await (from e in _dbContext.performance_event
                                       join d in _dbContext.driver on e.driver_id equals d.driver_id
                                       where e.event_id == eventId && e.company_id == companyId
                                       select new
                                       {
                                           Event = e,
                                           DriverName = d.driver_name
                                       }).FirstOrDefaultAsync();

                if (eventData == null)
                {
                    _logger.LogWarning("No incident found for event {EventId} and company {CompanyId}", eventId, companyId);
                    return null;
                }

                var incident = await _dbContext.incident
                    .FirstOrDefaultAsync(i => i.event_id == eventId);

                if (incident == null)
                {
                    _logger.LogWarning("No incident data found for event {EventId}", eventId);
                    return null;
                }

                var incidentTypes = await (from itir in _dbContext.incident_type_incident_relationship
                                           join it in _dbContext.incident_type on itir.incid_type_id equals it.incid_type_id
                                           where itir.incident_id == incident.incident_id
                                           select it.incid_type_name).ToListAsync();

                var equipmentDamages = await (from edr in _dbContext.incident_equip_damage_relationship
                                              join ed in _dbContext.incident_equip_damage on edr.incid_equip_id equals ed.incid_equip_id
                                              where edr.incident_id == incident.incident_id
                                              select ed.incid_equip_name).ToListAsync();

                var images = await _dbContext.incident_pictures
                    .Where(ip => ip.incident_id == incident.incident_id)
                    .Select(ip => ip.picture_url)
                    .ToListAsync();

                var documents = await _dbContext.incident_doc
                    .Where(id => id.incident_id == incident.incident_id)
                    .Select(id => new IncidentDocDto
                    {
                        DocPath = id.file_path,
                        DocNumber = id.doc_number
                    }).ToListAsync();

                var claims = await _dbContext.claim
                          .Where(c => c.event_id == eventId)
                          .ToListAsync();

                var incidentDetails = new IncidentDetailsDto
                {
                    TruckId = eventData.Event.truck_id,
                    DriverName = eventData.DriverName,
                    TrailerId = eventData.Event.trailer_id,
                    EventType = eventData.Event.event_type.ToString(),
                    Location = eventData.Event.location,
                    Authority = eventData.Event.authority.ToString(),
                    EventDate = eventData.Event.date,
                    Description = eventData.Event.description,
                    LoadId = (int)eventData.Event.load_id,
                    EventFee = eventData.Event.event_fees,
                    FeesPaidBy = eventData.Event.fees_paid_by.ToString(),
                    CompanyFeeApplied = eventData.Event.company_fee_applied,
                    CompanyFeeAmount = eventData.Event.company_fee_amount,
                    //CompanyFeeStatementDate = (DateTime)eventData.Event.company_fee_statement_date,
                    CompanyFeeStatementDate = eventData.Event.company_fee_statement_date ?? DateTime.MinValue,
                    IncidentTypes = incidentTypes,
                    EquipmentDamages = equipmentDamages,
                    Images = images,
                    Docs = documents,
                    Claims = claims
                };

                _logger.LogInformation("Incident details fetched successfully for event {EventId} and company {CompanyId}", eventId, companyId);

                return incidentDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching incident details for event {EventId} and company {CompanyId}", eventId, companyId);
                throw;
            }
        }

        public async Task<int> CreateIncidentAsync(Incident incident)
        {
            try
            {
                _logger.LogInformation("Creating a new incident.");

                _dbContext.incident.Add(incident);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Incident created successfully with ID: {IncidentId}", incident.incident_id);
                return incident.incident_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating incident.");
                throw;
            }
        }

        public async Task<bool> CreateIncidentWithTransactionAsync(
            PerformanceEvents performanceEvents,
            IncidentInfoDto incidentInfo,
            IncidentEventInfoDto eventInfo,
            List<IFormFile> images,
            List<IncidentDocumentDto> docs,
            List<Claims> incidentClaims,
            int companyId)
        {
            // Use the execution strategy provided by the database context
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Starting transaction to create incident for company {CompanyId}.", companyId);

                    // 1. Insert into performance_event
                    await _dbContext.performance_event.AddAsync(performanceEvents);
                    await _dbContext.SaveChangesAsync();
                    int eventId = performanceEvents.event_id;

                    _logger.LogInformation("Performance event created with ID: {EventId}", eventId);

                    // 2. Insert into incident
                    var incident = new Incident
                    {
                        event_id = eventId,
                    };

                    await _dbContext.incident.AddAsync(incident);
                    await _dbContext.SaveChangesAsync();
                    int incidentId = incident.incident_id;

                    // 3. Insert into incident_type_incident_relationship
                    var allIncidentTypes = await _dbContext.incident_type.ToListAsync();
                    foreach (var incidentTypeId in incidentInfo.IncidentTypeIds)
                    {
                        var incidentType = allIncidentTypes.FirstOrDefault(x => x.incid_type_id == incidentTypeId);

                        if (incidentType != null)
                        {
                            var relationship = new IncidentTypeIncidentRelationship
                            {
                                incident_id = incidentId,
                                incid_type_id = incidentType.incid_type_id
                            };
                            await _dbContext.incident_type_incident_relationship.AddAsync(relationship);
                        }
                    }

                    // 4. Insert into incident_equip_damage_relationship
                    var allEquipDamages = await _dbContext.incident_equip_damage.ToListAsync();
                    foreach (var equipDamageId in incidentInfo.EquipmentDamageIds)
                    {
                        var equipDamageType = allEquipDamages.FirstOrDefault(x => x.incid_equip_id == equipDamageId);

                        if (equipDamageType != null)
                        {
                            var equipDamage = new IncidentEquipDamageRelationship
                            {
                                incident_id = incidentId,
                                incid_equip_id = equipDamageType.incid_equip_id
                            };
                            await _dbContext.incident_equip_damage_relationship.AddAsync(equipDamage);
                        }
                    }

                    // 5. Insert Images
                    if (images != null)
                    {
                        foreach (var img in images)
                        {
                            string url = await SaveIncidentImageAsync(companyId, eventInfo.DriverId, incidentId, img);

                            var pic = new IncidentPicture
                            {
                                incident_id = incidentId,
                                picture_url = url
                            };
                            await _dbContext.incident_pictures.AddAsync(pic);
                        }
                    }

                    // 6. Insert Documents
                    if (docs != null)
                    {
                        foreach (var doc in docs)
                        {
                            string docPath = await SaveIncidentDocumentAsync(companyId, eventInfo.DriverId, incidentId, doc);

                            var incidentDoc = new IncidentDoc
                            {
                                incident_id = incidentId,
                                doc_type_id = doc.DocTypeId,
                                doc_number = doc.DocNumber,
                                file_path = docPath,
                                status = "Active"
                            };
                            await _dbContext.incident_doc.AddAsync(incidentDoc);
                        }
                    }

                    // 7. Create Claims
                    foreach (var claim in incidentClaims)
                    {
                        _logger.LogInformation("Creating claim record for Incident ID {IncidentId}.", incidentId);
                        var claimRecord = new Claims
                        {
                            event_id = eventId,
                            claim_type = claim.claim_type,
                            status = claim.status,
                            claim_amount = claim.claim_amount,
                            claim_description = claim.claim_description,
                            created_at = DateTime.UtcNow,
                            updated_at = DateTime.UtcNow,
                        };
                        await _claimRepository.CreateClaimAsync(claimRecord);
                    }

                    // Save all
                    await _dbContext.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Transaction committed successfully for incident {IncidentId}", incidentId);
                    _logger.LogInformation("Incident created with ID: {IncidentId} for event {EventId}", incidentId, eventId);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transaction failed for creating incident. Rolling back...");
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task UpdateIncidentTypesAsync(int eventId, List<int?> newTypes)
        {
            try
            {
                // Get all incident_ids for the given event
                var incidentIds = await _dbContext.incident
                    .Where(i => i.event_id == eventId)
                    .Select(i => i.incident_id)
                    .ToListAsync();

                if (!incidentIds.Any())
                    return;

                // Validate that all provided newTypes exist
                var validTypes = await _dbContext.incident_type
                    .Where(t => newTypes.Contains(t.incid_type_id))
                    .Select(t => t.incid_type_id)
                    .ToListAsync();

                var invalidIds = newTypes
                    .Where(id => id.HasValue && !validTypes.Contains(id.Value))
                    .ToList();

                if (invalidIds.Any())
                {
                    var invalidStr = string.Join(", ", invalidIds);
                    throw new ArgumentException($"Invalid Incident Type ID(s): {invalidStr}");
                }

                // Get all existing relationships for incidents related to the event
                var existingRelations = await _dbContext.incident_type_incident_relationship
                    .Where(r => incidentIds.Contains(r.incident_id))
                    .ToListAsync();

                // Get existing incident_type_ids
                var existingTypeIds = existingRelations.Select(r => r.incid_type_id).ToHashSet();
                var newTypeIdSet = validTypes.ToHashSet();

                var toDelete = existingRelations
                    .Where(r => !newTypeIdSet.Contains(r.incid_type_id))
                    .ToList();

                var toAdd = new List<IncidentTypeIncidentRelationship>();

                foreach (var incidentId in incidentIds)
                {
                    foreach (var newTypeId in newTypeIdSet)
                    {
                        if (!existingRelations.Any(r => r.incident_id == incidentId && r.incid_type_id == newTypeId))
                        {
                            toAdd.Add(new IncidentTypeIncidentRelationship
                            {
                                incident_id = incidentId,
                                incid_type_id = newTypeId
                            });
                        }
                    }
                }

                _dbContext.incident_type_incident_relationship.RemoveRange(toDelete);
                await _dbContext.incident_type_incident_relationship.AddRangeAsync(toAdd);

                await _dbContext.SaveChangesAsync();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating incident types.");
                throw;
            }
            catch (Exception ex)
            {
                // You can log the exception here or rethrow a custom one
                throw new ApplicationException("Error updating incident types.", ex);
            }
        }

        public async Task UpdateEquipmentDamagesAsync(int eventId, List<int?> newDamages)
        {
            try
            {
                // Get all incident_ids related to the event
                var incidentIds = await _dbContext.incident
                    .Where(i => i.event_id == eventId)
                    .Select(i => i.incident_id)
                    .ToListAsync();

                if (!incidentIds.Any())
                    return;

                // Get all valid equipment damages from DB
                var allEquipments = await _dbContext.incident_equip_damage
                    .Where(e => newDamages.Contains(e.incid_equip_id))
                    .ToListAsync();

                var validEquipIds = allEquipments.Select(e => e.incid_equip_id).ToHashSet();

                // Validation: check if any of the provided IDs are invalid
                var invalidIds = newDamages.Where(id => !validEquipIds.Contains(id.Value)).ToList();

                if (invalidIds.Any())
                {
                    var invalidStr = string.Join(", ", invalidIds);
                    throw new ArgumentException($"Invalid Equipment Damage ID(s): {invalidStr}");
                }

                // Get all existing relationships for the incidents
                var existingRelations = await _dbContext.incident_equip_damage_relationship
                    .Where(r => incidentIds.Contains(r.incident_id))
                    .ToListAsync();

                var existingEquipIds = existingRelations.Select(r => r.incid_equip_id).ToHashSet();

                // Determine what to delete
                var toDelete = existingRelations
                    .Where(r => !validEquipIds.Contains(r.incid_equip_id))
                    .ToList();

                // Determine what to add
                var toAdd = new List<IncidentEquipDamageRelationship>();

                foreach (var incidentId in incidentIds)
                {
                    foreach (var equipId in validEquipIds)
                    {
                        if (!existingRelations.Any(r => r.incident_id == incidentId && r.incid_equip_id == equipId))
                        {
                            toAdd.Add(new IncidentEquipDamageRelationship
                            {
                                incident_id = incidentId,
                                incid_equip_id = equipId
                            });
                        }
                    }
                }

                _dbContext.incident_equip_damage_relationship.RemoveRange(toDelete);
                await _dbContext.incident_equip_damage_relationship.AddRangeAsync(toAdd);

                await _dbContext.SaveChangesAsync();
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Validation error while updating equipment damages types.");
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error updating equipment damages.", ex);
            }
        }


        private async Task<string> SaveIncidentImageAsync(int companyId, int driverId, int incidentId, IFormFile picture)
        {
            _logger.LogInformation("Saving incident image for Company: {CompanyId}, Driver: {DriverId}, Incident: {IncidentId}", companyId, driverId, incidentId);

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server incident directory is not configured.");
                throw new InvalidOperationException("Server incident directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Incident", companyId.ToString(), "Incident_Doc", incidentId.ToString());

            // Ensure the directory exists
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    _logger.LogInformation("Created directory: {FolderPath}", folderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {FolderPath}", folderPath);
                throw;
            }

            // Generate the file name
            string fileExtension = Path.GetExtension(picture.FileName);  // Get the extension (e.g., ".png")
            string fileName = $"{incidentId}_{Guid.NewGuid()}{fileExtension}"; // Generate a unique filename
            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await picture.CopyToAsync(stream);
                }

                _logger.LogInformation("Image successfully saved at: {FilePath}", filePath);
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

            // Construct the public URL dynamically
            string incidentImageUrl = $"{serverUrl}/Documents/Event/Incident/{companyId}/Incident_Doc/{incidentId}/{fileName}";
            _logger.LogInformation("Generated public URL for incident image: {IncidentImageUrl}", incidentImageUrl);

            return incidentImageUrl;
        }

        private async Task<string> SaveIncidentDocumentAsync(int companyId, int driverId, int incidentId, IncidentDocumentDto doc)
        {
            _logger.LogInformation("Starting SaveIncidentDocumentAsync for CompanyId: {CompanyId}, DriverId: {DriverId}, incidentId: {incidentId}", companyId, driverId, incidentId);

            // Validate the base directory from configuration
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Incident", companyId.ToString(), "Incident_Doc", incidentId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Created directory: {FolderPath}", folderPath);
                Directory.CreateDirectory(folderPath);
            }

            // Generate the file name
            var fileName = $"{incidentId}_{doc.DocTypeId}_{driverId}.pdf";
            var filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await doc.File.CopyToAsync(stream);
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
            string incidentDocumentUrl = $"{serverUrl}/Documents/Event/Incident/{companyId}/Incident_Doc/{incidentId}/{fileName}";
            _logger.LogInformation("Generated document URL: {IncidentDocumentUrl}", incidentDocumentUrl);

            return incidentDocumentUrl;
        }


        public async Task<Incident> GetIncidentByEventId(int eventId) 
        {
            return await _dbContext.incident.Where(x => x.event_id == eventId).FirstOrDefaultAsync();
        }
    }
}
