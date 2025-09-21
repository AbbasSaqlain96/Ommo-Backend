using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Parsers;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IConfiguration _configuration;
        private readonly IIncidentPicturesRepository _incidentPicturesRepository;
        private readonly ITruckRepository _truckRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly IIncidentTypeRepository _incidentTypeRepository;
        private readonly IIncidentEquipDamageRepository _incidentEquipDamageRepository;
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IIncidentDocRepository _incidentDocRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncidentService> _logger;

        public IncidentService(
            IIncidentRepository incidentRepository,
            IEventRepository eventRepository,
            IConfiguration configuration,
            IIncidentPicturesRepository incidentPicturesRepository,
            ITruckRepository truckRepository,
            IDriverRepository driverRepository,
            ICarrierRepository carrierRepository,
            IVehicleRepository vehicleRepository,
            ITrailerRepository trailerRepository,
            IIncidentTypeRepository incidentTypeRepository,
            IIncidentEquipDamageRepository incidentEquipDamageRepository,
            IPerformanceRepository performanceRepository,
            IIncidentDocRepository incidentDocRepository,
            IClaimRepository claimRepository,
            AppDbContext dbContext,
            IUnitOfWork unitOfWork,
            ILogger<IncidentService> logger)
        {
            _incidentRepository = incidentRepository;
            _eventRepository = eventRepository;
            _configuration = configuration;
            _incidentPicturesRepository = incidentPicturesRepository;
            _truckRepository = truckRepository;
            _driverRepository = driverRepository;
            _carrierRepository = carrierRepository;
            _vehicleRepository = vehicleRepository;
            _trailerRepository = trailerRepository;
            _incidentTypeRepository = incidentTypeRepository;
            _incidentEquipDamageRepository = incidentEquipDamageRepository;
            _performanceRepository = performanceRepository;
            _incidentDocRepository = incidentDocRepository;
            _claimRepository = claimRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResponse<IncidentDetailsDto>> GetIncidentDetailsAsync(int eventId, int companyId)
        {
            _logger.LogInformation("Fetching incident details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);

            try
            {
                // Ensure the event belongs to the same company
                var eventBelongsToCompany = await _incidentRepository.CheckEventCompany(eventId, companyId);
                if (!eventBelongsToCompany)
                {
                    _logger.LogWarning("Event {EventId} does not belong to Company {CompanyId}", eventId, companyId);
                    return ServiceResponse<IncidentDetailsDto>.ErrorResponse("No incident info found for the given event ID.", 400);
                }

                // Fetch incident details
                var incidentDetails = await _incidentRepository.FetchIncidentDetailsAsync(eventId, companyId);

                if (incidentDetails == null)
                {
                    _logger.LogWarning("No incident details found for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                    return ServiceResponse<IncidentDetailsDto>.ErrorResponse("No incident info found for the given event ID.", 400);
                }

                _logger.LogInformation("Incident details retrieved successfully for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<IncidentDetailsDto>.SuccessResponse(incidentDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve incident details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<IncidentDetailsDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<IncidentCreationResult>> CreateIncidentAsync(
            int companyId, CreateIncidentRequest incidentRequest)
        {
            _logger.LogInformation("Creating incident for CompanyId: {CompanyId}", companyId);

            try
            {
                // Validate Truck
                var truck = await _truckRepository.GetByIdAsync(incidentRequest.EventInfo.TruckId);
                if (truck == null)
                {
                    _logger.LogWarning("Truck {TruckId} not found", incidentRequest.EventInfo.TruckId);
                    return ServiceResponse<IncidentCreationResult>.ErrorResponse("No Truck found for the provided Truck ID", 400);
                }

                var vehicle = await _vehicleRepository.GetByIdAsync(truck.vehicle_id);
                if (vehicle == null || !vehicle.carrier_id.HasValue)
                {
                    _logger.LogWarning("Invalid vehicle or carrier not found for Truck {TruckId}", incidentRequest.EventInfo.TruckId);
                    return ServiceResponse<IncidentCreationResult>.ErrorResponse("Truck belongs to another company.", 400);
                }

                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null || carrier.company_id != companyId)
                {
                    _logger.LogWarning("Carrier mismatch for company ID: {CompanyId}", companyId);
                    return ServiceResponse<IncidentCreationResult>.ErrorResponse("Truck belongs to another company.", 400);
                }

                // Validate Driver
                var driver = await _driverRepository.GetByIdAsync(incidentRequest.EventInfo.DriverId);
                if (driver == null)
                {
                    _logger.LogWarning("Driver {DriverId} not found for Company {CompanyId}", incidentRequest.EventInfo.DriverId, companyId);
                    return ServiceResponse<IncidentCreationResult>.ErrorResponse("No driver found for the provided Driver ID", 400);
                }

                if (driver.company_id != companyId)
                {
                    _logger.LogWarning("Driver {DriverId} belongs to a different company {CompanyId}", driver.driver_id, driver.company_id);
                    return ServiceResponse<IncidentCreationResult>.ErrorResponse("No driver found for the provided Driver ID", 400);
                }

                // Validate Trailer if provided
                if (incidentRequest.EventInfo.TrailerId.HasValue)
                {
                    var trailer = await _trailerRepository.GetByIdAsync(incidentRequest.EventInfo.TrailerId.Value);
                    if (trailer == null)
                    {
                        _logger.LogWarning("Trailer {TrailerId} not found", incidentRequest.EventInfo.TrailerId.Value);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("No trailer found for the provided Trailer ID", 400);
                    }

                    var trailerVehicle = await _vehicleRepository.GetByIdAsync(trailer.vehicle_id);
                    if (trailerVehicle == null || !trailerVehicle.carrier_id.HasValue)
                    {
                        _logger.LogWarning("Invalid vehicle or carrier not found for Trailer {TrailerId}", incidentRequest.EventInfo.TrailerId.Value);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }

                    var trailerCarrier = await _carrierRepository.GetByIdAsync(trailerVehicle.carrier_id.Value);
                    if (trailerCarrier == null || trailerCarrier.company_id != companyId)
                    {
                        _logger.LogWarning("Carrier mismatch for trailer CompanyId: {CompanyId}", companyId);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }
                }

                //// Validate Load if provided
                //if (incidentRequest.EventInfo.LoadId.HasValue)
                //{
                //    var load = await _loadRepository.GetByIdAsync(incidentRequest.EventInfo.LoadId.Value);
                //    if (load == null || load.company_id != companyId)
                //    {
                //        _logger.LogWarning("Load {LoadId} not found or does not belong to CompanyId: {CompanyId}", incidentRequest.EventInfo.LoadId, companyId);
                //        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Load belongs to another company.", 400);
                //    }
                //}

                // Validate IncidentType
                var allIncidentTypes = await _incidentTypeRepository.GetAllAsync();
                foreach (var incidentTypeId in incidentRequest.IncidentInfo.IncidentTypeIds)
                {
                    var incidentType = allIncidentTypes.FirstOrDefault(x => x.incid_type_id == incidentTypeId);

                    if (incidentType == null)
                    {
                        _logger.LogWarning("Invalid incident type id: {IncidentTypeId}", incidentTypeId);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Invalid incident type id selected.", 400);
                    }

                    var _incidentType = await _incidentTypeRepository.GetByIdAsync(incidentType.incid_type_id);

                    if (_incidentType == null)
                    {
                        _logger.LogWarning("IncidentType {IncidentTypeId} not found", incidentType.incid_type_id);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Invalid incident type selected.", 400);
                    }
                }

                // Validate EquipmentDamage if provided
                var allIncidentEquipDamage = await _incidentEquipDamageRepository.GetAllAsync();
                foreach (var incidEquipDamageId in incidentRequest.IncidentInfo.EquipmentDamageIds)
                {
                    var incidentincidEquipDamage = allIncidentEquipDamage.FirstOrDefault(x => x.incid_equip_id == incidEquipDamageId);

                    if (incidentincidEquipDamage == null)
                    {
                        _logger.LogWarning("Invalid equipment damage id: {IncidentEquipDamageId}", incidEquipDamageId);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Invalid equipment damage id selected.", 400);
                    }

                    var equipmentDamage = await _incidentEquipDamageRepository.GetByIdAsync(incidentincidEquipDamage.incid_equip_id);

                    if (equipmentDamage == null)
                    {
                        _logger.LogWarning("EquipmentDamage {EquipmentDamageId} not found", incidentincidEquipDamage.incid_equip_id);
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Invalid equipment damage selected.", 400);
                    }
                }

                // Validate image formats (if applicable)
                if (incidentRequest.Images != null && incidentRequest.Images.Any())
                {
                    var allowedMimeTypes = new[] { "image/jpg", "image/jpeg", "image/png", "image/webp" };
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                    foreach (var image in incidentRequest.Images)
                    {
                        if (image == null)
                            continue;

                        var contentType = image.ContentType?.ToLower().Trim();
                        var extension = Path.GetExtension(image.FileName)?.ToLower().Trim();

                        if (!allowedMimeTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
                        {
                            _logger.LogWarning("Invalid image format detected. ContentType: {ContentType}, Extension: {Extension}", contentType, extension);
                            return ServiceResponse<IncidentCreationResult>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                        }
                    }
                }

                // Compose the document internally
                var documents = new List<IncidentDocumentDto>();

                if (incidentRequest.DocFile != null && !string.IsNullOrWhiteSpace(incidentRequest.DocNumber))
                {
                    var doc = new IncidentDocumentDto
                    {
                        DocTypeId = 28,
                        DocNumber = incidentRequest.DocNumber,
                        File = incidentRequest.DocFile
                    };

                    // Validate format
                    if (!ValidationHelper.IsValidDocumentFormat(doc.File, new[] { ".pdf", ".doc", ".docx" }))
                    {
                        return ServiceResponse<IncidentCreationResult>.ErrorResponse("Invalid document format. Only PDF, DOC, and DOCX formats are allowed.", 400);
                    }

                    documents.Add(doc);
                }

                if (!Enum.TryParse(incidentRequest.EventInfo.Authority, true, out EventAuthority eventAuthority))
                    throw new ArgumentException($"Invalid event authority: {incidentRequest.EventInfo.Authority}");

                if (!Enum.TryParse(incidentRequest.EventInfo.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                    throw new ArgumentException($"Invalid fees paid by: {incidentRequest.EventInfo.FeesPaidBy}");

                var companyFeeApplied = incidentRequest.EventInfo.CompanyFeeApplied?.ToLower() == "yes";

                // Create Performance Event
                var performanceEvent = new PerformanceEvents
                {
                    event_type = EventType.incident,
                    driver_id = incidentRequest.EventInfo.DriverId,
                    truck_id = incidentRequest.EventInfo.TruckId,
                    trailer_id = incidentRequest.EventInfo.TrailerId ?? 0,
                    location = incidentRequest.EventInfo.Location,
                    authority = eventAuthority,
                    description = incidentRequest.IncidentInfo.Description,
                    load_id = incidentRequest.EventInfo.LoadId ?? 0,
                    event_fees = incidentRequest.EventInfo.EventFee ?? 0,
                    fees_paid_by = feesPaidBy,
                    company_fee_applied = companyFeeApplied,
                    company_fee_amount = incidentRequest.EventInfo.CompanyFeeAmount ?? 0,
                    company_fee_statement_date = incidentRequest.EventInfo.CompanyFeeStatementDate,
                    date = incidentRequest.EventInfo.EventDate,
                    company_id = companyId
                };

                _logger.LogInformation("Creating incident record in database for CompanyId: {CompanyId}", companyId);

                // Create Incident & Related Data in a Transaction
                bool isCreated = await _incidentRepository.CreateIncidentWithTransactionAsync(
                    performanceEvent, incidentRequest.IncidentInfo, incidentRequest.EventInfo, incidentRequest.Images, documents, ClaimParser.DeserializeClaims(incidentRequest.ClaimInfoJson), companyId);

                if (!isCreated)
                {
                    _logger.LogError("Incident creation failed in DB for company ID: {CompanyId}", companyId);
                    return ServiceResponse<IncidentCreationResult>.ErrorResponse("Failed to create the incident.", 400);
                }

                _logger.LogInformation("Incident created successfully for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IncidentCreationResult>.SuccessResponse(null, "Incident created successfully.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating incident.");
                return ServiceResponse<IncidentCreationResult>.ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating an incident for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IncidentCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<IncidentUpdateResult>> UpdateIncidentAsync(int companyId, UpdateIncidentRequest request)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var eventEntity = await _performanceRepository.GetByIdAsync(request.EventId);
                    if (eventEntity == null || eventEntity.company_id != companyId || eventEntity.event_type != EventType.incident)
                    {
                        return ServiceResponse<IncidentUpdateResult>.ErrorResponse("No incident found for the provided Event ID.", 400);
                    }

                    // Truck
                    if (request.TruckId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTruckOwnershipAsync(_truckRepository, request.TruckId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<IncidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var truck = (Truck)entity!;
                        eventEntity.truck_id = truck.truck_id;
                    }

                    // Driver
                    if (request.DriverId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateDriverOwnershipAsync(_driverRepository, request.DriverId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<IncidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var driver = (Driver)entity!;
                        eventEntity.driver_id = driver.driver_id;
                    }

                    // Trailer
                    if (request.TrailerId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTrailerOwnershipAsync(_trailerRepository, request.TrailerId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<IncidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var trailer = (Trailer)entity!;
                        eventEntity.trailer_id = trailer.trailer_id;
                    }

                    if (!string.IsNullOrEmpty(request.Authority))
                    {
                        if (!Enum.TryParse(request.Authority, true, out EventAuthority eventAuthority))
                            throw new ArgumentException($"Invalid event authority: {request.Authority}");

                        eventEntity.authority = eventAuthority;
                    }

                    if (!string.IsNullOrEmpty(request.FeesPaidBy))
                    {
                        if (!Enum.TryParse(request.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                            throw new ArgumentException($"Invalid fees paid by: {request.FeesPaidBy}");

                        eventEntity.fees_paid_by = feesPaidBy;
                    }

                    var companyFeeApplied = request.CompanyFeeApplied?.ToLower() == "yes";

                    if (!string.IsNullOrEmpty(request.Location))
                        eventEntity.location = request.Location;

                    if (!string.IsNullOrEmpty(request.Description))
                        eventEntity.description = request.Description;

                    // Event Date Validation
                    if (request.EventDate.HasValue)
                    {
                        if (request.EventDate.Value > DateTime.UtcNow)
                        {
                            return ServiceResponse<IncidentUpdateResult>.ErrorResponse("Event date cannot be in the future.", 400);
                        }

                        eventEntity.date = request.EventDate.Value;
                    }

                    if (request.LoadId.HasValue)
                        eventEntity.load_id = request.LoadId.Value;

                    if (request.EventFee.HasValue)
                        eventEntity.event_fees = request.EventFee.Value;

                    if (!string.IsNullOrEmpty(request.CompanyFeeApplied))
                        eventEntity.company_fee_applied = companyFeeApplied;

                    if (request.CompanyFeeAmount.HasValue)
                        eventEntity.company_fee_amount = request.CompanyFeeAmount.Value;

                    if (request.CompanyFeeStatementDate.HasValue)
                        eventEntity.company_fee_statement_date = request.CompanyFeeStatementDate;

                    await _performanceRepository.UpdateAsync(eventEntity);

                    var _event = await _eventRepository.GetByIdAsync(eventEntity.event_id);

                    // Relationships
                    if (request.IncidentTypeIds != null)
                    {
                        await _incidentRepository.UpdateIncidentTypesAsync(request.EventId, request.IncidentTypeIds);
                    }

                    if (request.EquipmentDamageIds != null)
                    {
                        await _incidentRepository.UpdateEquipmentDamagesAsync(request.EventId, request.EquipmentDamageIds);
                    }

                    var incident = await _incidentRepository.GetIncidentByEventId(request.EventId);

                    int incidentId = 0;
                    if (incident != null) 
                    {
                        incidentId = incident.incident_id;
                    }

                    int driverId = _event.driver_id;

                    // Validate and update images
                    if (request.Images != null && request.Images.Any())
                    {
                        if (!ValidationHelper.AreValidImageFormats(request.Images))
                        {
                            return ServiceResponse<IncidentUpdateResult>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                        }

                        await _incidentPicturesRepository.UpdateImagesAsync(request.EventId, request.Images);
                    }

                    // Validate and update documents
                    if (request.DocFile != null)
                    {
                        if (!ValidationHelper.IsValidDocumentFormat(request.DocFile, new[] { ".pdf", ".doc", ".docx" }))
                        {
                            return ServiceResponse<IncidentUpdateResult>.ErrorResponse("Invalid document format. Only PDF, DOC, and DOCX formats are allowed.", 400);
                        }

                        var updateDoc = new UpdateDocumentRequest
                        {
                            DocTypeId = 28, // fixed "incident_doc"
                            DocNumber = request.DocNumber,
                            File = request.DocFile
                        };

                        await _incidentDocRepository.UpdateDocsAsync(request.EventId, incidentId, driverId, new List<UpdateDocumentRequest?> { updateDoc });
                    }

                    // Claims
                    if (request.ClaimInfoJson != null)
                    {
                        var claimDtos = ClaimParser.DeserializeClaims(request.ClaimInfoJson);

                        var claims = claimDtos.Select(r => new Claims
                        {
                            event_id = r.event_id,
                            claim_description = r.claim_description,
                            claim_type = r.claim_type,
                            claim_amount = r.claim_amount,
                            status = r.status,
                            updated_at = r.updated_at
                        }).ToList();

                        await _claimRepository.UpdateClaimsAsync(request.EventId, claims);
                    }

                    await transaction.CommitAsync();

                    return ServiceResponse<IncidentUpdateResult>.SuccessResponse(null, "Incident updated successfully.");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Validation error while updating incident.");
                    return ServiceResponse<IncidentUpdateResult>.ErrorResponse(ex.Message, 400);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error updating incident");
                    return ServiceResponse<IncidentUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }
    }
}
