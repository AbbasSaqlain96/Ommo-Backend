using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Linq;

namespace OmmoBackend.Services.Implementations
{
    public class DotInspectionService : IDotInspectionService
    {
        private readonly ITruckRepository _truckRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IViolationRepository _violationRepository;
        private readonly IDotInspectionRepository _dotInspectionRepository;
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IDocInspectionRepository _docInspectionRepository;
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DotInspectionService> _logger;

        public DotInspectionService(
            ITruckRepository truckRepository,
            IDriverRepository driverRepository,
            ICarrierRepository carrierRepository,
            IVehicleRepository vehicleRepository,
            ITrailerRepository trailerRepository,
            IViolationRepository violationRepository,
            IDotInspectionRepository dotInspectionRepository,
            IPerformanceRepository performanceRepository,
            IDocInspectionRepository docInspectionRepository,
            AppDbContext dbContext,
            IUnitOfWork unitOfWork,
            ILogger<DotInspectionService> logger)
        {
            _truckRepository = truckRepository;
            _driverRepository = driverRepository;
            _carrierRepository = carrierRepository;
            _vehicleRepository = vehicleRepository;
            _trailerRepository = trailerRepository;
            _violationRepository = violationRepository;
            _dotInspectionRepository = dotInspectionRepository;
            _performanceRepository = performanceRepository;
            _docInspectionRepository = docInspectionRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResponse<DotInspectionCreationResult>> CreateDotInspectionAsync(int companyId, CreateDotInspectionRequest request)
        {
            try
            {
                _logger.LogInformation("Creating dot inspection for Company ID: {CompanyId}", companyId);

                // Event Date Validation
                if (request.dotInspectionEventInfoDto.EventDate.Date > DateTime.UtcNow.Date)
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Event date cannot be in the future.", 400);

                // Document Null Check
                if (request.docInspectionDocumentsDto.DocInspectionDoc == null)
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Document is required.", 400);

                // Document Format Validation
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                // Null check for file
                if (request.docInspectionDocumentsDto.DocInspectionDoc != null)
                {
                    var fileName = request.docInspectionDocumentsDto.DocInspectionDoc.FileName?.Trim();

                    // Only validate format if fileName is not null or empty
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var extension = Path.GetExtension(fileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Invalid document format. Only PDF, DOC, and DOCX formats are allowed", 400);
                        }
                    }
                }

                // Truck Validation
                var truck = await _truckRepository.GetByIdAsync(request.dotInspectionEventInfoDto.TruckId);
                if (truck == null)
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("No Truck found for the provided Truck ID", 400);

                var vehicle = await _vehicleRepository.GetByIdAsync(truck.vehicle_id);
                if (vehicle == null || !vehicle.carrier_id.HasValue)
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Truck belongs to another company.", 400);

                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null || carrier.company_id != companyId)
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Truck belongs to another company.", 400);

                // Driver Validation
                var driver = await _driverRepository.GetByIdAsync(request.dotInspectionEventInfoDto.DriverId);
                if (driver == null || driver.company_id != companyId)
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("No driver found for the provided Driver ID", 400);

                // Validate Trailer if provided
                if (request.dotInspectionEventInfoDto.TrailerId.HasValue)
                {
                    var trailer = await _trailerRepository.GetByIdAsync(request.dotInspectionEventInfoDto.TrailerId.Value);
                    if (trailer == null)
                    {
                        _logger.LogWarning("Trailer {TrailerId} not found", request.dotInspectionEventInfoDto.TrailerId.Value);
                        return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("No trailer found for the provided Trailer ID", 400);
                    }

                    var trailerVehicle = await _vehicleRepository.GetByIdAsync(trailer.vehicle_id);
                    if (trailerVehicle == null || !trailerVehicle.carrier_id.HasValue)
                    {
                        _logger.LogWarning("Invalid vehicle or carrier not found for Trailer {TrailerId}", request.dotInspectionEventInfoDto.TrailerId.Value);
                        return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }

                    var trailerCarrier = await _carrierRepository.GetByIdAsync(trailerVehicle.carrier_id.Value);
                    if (trailerCarrier == null || trailerCarrier.company_id != companyId)
                    {
                        _logger.LogWarning("Carrier mismatch for trailer CompanyId: {CompanyId}", companyId);
                        return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }
                }

                // Violations Validation
                var violationIds = request.Violations ?? new List<int>();
                var existingViolations = await _violationRepository.GetByIdsAsync(violationIds);
                if (existingViolations.Count != violationIds.Count)
                {
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Selected Violations does not exist", 400);
                }

                if (!Enum.TryParse(request.dotInspectionEventInfoDto.Authority, true, out EventAuthority eventAuthority))
                    throw new ArgumentException($"Invalid event authority: {request.dotInspectionEventInfoDto.Authority}");

                if (!Enum.TryParse(request.dotInspectionEventInfoDto.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                    throw new ArgumentException($"Invalid fees paid by: {request.dotInspectionEventInfoDto.FeesPaidBy}");

                var companyFeeApplied = request.dotInspectionEventInfoDto.CompanyFeeApplied?.ToLower() == "yes";


                if (!Enum.TryParse(request.docInspectionDto.Status, true, out DocInspectionStatus docInspectionStatus))
                    throw new ArgumentException($"Invalid doc inspection status: {request.docInspectionDto.Status}");

                if (!Enum.IsDefined(typeof(InspectionLevel), request.docInspectionDto.InspectionLevel))
                    throw new ArgumentException($"Invalid doc inspection level: {request.docInspectionDto.InspectionLevel}");

                int inspectionLevel = request.docInspectionDto.InspectionLevel;

                if (!Enum.TryParse(request.docInspectionDto.Citation, true, out CitationStatus citationStatus))
                    throw new ArgumentException($"Invalid doc citation status: {request.docInspectionDto.Citation}");

                // Prepare Performance Event Data
                var performanceEvents = new PerformanceEvents
                {
                    truck_id = request.dotInspectionEventInfoDto.TruckId,
                    driver_id = request.dotInspectionEventInfoDto.DriverId,
                    trailer_id = request.dotInspectionEventInfoDto.TrailerId ?? 0,
                    location = request.dotInspectionEventInfoDto.Location,
                    event_type = EventType.dot_inspection,
                    authority = eventAuthority,
                    date = request.dotInspectionEventInfoDto.EventDate,
                    description = request.dotInspectionEventInfoDto.Description,
                    load_id = request.dotInspectionEventInfoDto.LoadId ?? 0,
                    event_fees = request.dotInspectionEventInfoDto.EventFee,
                    fees_paid_by = feesPaidBy,
                    company_fee_applied = companyFeeApplied,
                    company_fee_amount = request.dotInspectionEventInfoDto.CompanyFeeAmount,
                    company_fee_statement_date = request.dotInspectionEventInfoDto.CompanyFeeStatementDate,
                    company_id = companyId
                };

                // Create Dot Inspection & Related Data in a Transaction
                bool isCreated = await _dotInspectionRepository.CreateDotInspectionWithTransactionAsync(
                    performanceEvents, request.docInspectionDto, request.docInspectionDocumentsDto, request.Violations!, companyId,
                     docInspectionStatus, inspectionLevel, citationStatus);

                if (!isCreated)
                {
                    _logger.LogError("Failed to create dot inspection for Company ID: {CompanyId}", companyId);
                    return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }

                _logger.LogInformation("Dot inspection created successfully for Company ID: {CompanyId}", companyId);
                return ServiceResponse<DotInspectionCreationResult>.SuccessResponse(null, "Dot Inspection created successfully.");

            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating DOT inspection.");
                return ServiceResponse<DotInspectionCreationResult>.ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateDotInspectionAsync.");
                return ServiceResponse<DotInspectionCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<DotInspectionDetailsDto>> GetDotInspectionDetailsAsync(int eventId, int companyId)
        {
            _logger.LogInformation("Fetching dot inspection details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);

            try
            {
                // Ensure the event belongs to the same company
                var eventBelongsToCompany = await _dotInspectionRepository.CheckEventCompany(eventId, companyId);
                if (!eventBelongsToCompany)
                {
                    _logger.LogWarning("Event {EventId} does not belong to Company {CompanyId}", eventId, companyId);
                    return ServiceResponse<DotInspectionDetailsDto>.ErrorResponse("No dot inspection found for the given event ID.", 400);
                }

                // Fetch dot inspection details
                var dotInspectionDetails = await _dotInspectionRepository.FetchDotInspectionDetailsAsync(eventId, companyId);
                if (dotInspectionDetails == null)
                {
                    _logger.LogWarning("No dot inspection details found for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                    return ServiceResponse<DotInspectionDetailsDto>.ErrorResponse("No dot inspection info found for the given event ID.", 400);
                }

                _logger.LogInformation("Dot inspection details retrieved successfully for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<DotInspectionDetailsDto>.SuccessResponse(dotInspectionDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dot inspection details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<DotInspectionDetailsDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<DotInspectionUpdateResult>> UpdateDotInspectionAsync(int companyId, UpdateDotInspectionRequest request)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var eventEntity = await _performanceRepository.GetByIdAsync(request.EventId);
                    if (eventEntity == null || eventEntity.company_id != companyId || eventEntity.event_type != EventType.dot_inspection)
                    {
                        return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse("No dot inspection found for the provided Event ID.", 400);
                    }

                    // Document Format Validation
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                    // Null check for file
                    if (request.DocInspectionDoc != null)
                    {
                        var fileName = request.DocInspectionDoc.FileName?.Trim();

                        // Only validate format if fileName is not null or empty
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            var extension = Path.GetExtension(fileName).ToLowerInvariant();

                            if (!allowedExtensions.Contains(extension))
                            {
                                return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse(
                                    "Invalid document format. Only PDF, DOC, and DOCX formats are allowed", 400);
                            }
                        }
                    }

                    // Truck
                    if (request.TruckId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTruckOwnershipAsync(_truckRepository, request.TruckId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var truck = (Truck)entity!;
                        eventEntity.truck_id = truck.truck_id;
                    }

                    // Driver
                    if (request.DriverId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateDriverOwnershipAsync(_driverRepository, request.DriverId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var driver = (Driver)entity!;
                        eventEntity.driver_id = driver.driver_id;
                    }

                    // Trailer
                    if (request.TrailerId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTrailerOwnershipAsync(_trailerRepository, request.TrailerId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var trailer = (Trailer)entity!;
                        eventEntity.trailer_id = trailer.trailer_id;
                    }

                    if (!string.IsNullOrEmpty(request.Location))
                        eventEntity.location = request.Location;

                    if (!string.IsNullOrEmpty(request.Authority))
                    {
                        if (!Enum.TryParse(request.Authority, true, out EventAuthority eventAuthority))
                            throw new ArgumentException($"Invalid event authority: {request.Authority}");

                        eventEntity.authority = eventAuthority;
                    }

                    // Event Date Validation
                    if (request.EventDate.HasValue)
                    {
                        if (request.EventDate.Value > DateTime.UtcNow)
                        {
                            return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse("Event date cannot be in the future.", 400);
                        }

                        eventEntity.date = request.EventDate.Value;
                    }

                    if (!string.IsNullOrEmpty(request.Description))
                        eventEntity.description = request.Description;

                    if (request.LoadId.HasValue)
                        eventEntity.load_id = request.LoadId.Value;

                    if (request.EventFee.HasValue)
                        eventEntity.event_fees = request.EventFee.Value;

                    if (!string.IsNullOrEmpty(request.FeesPaidBy))
                    {
                        if (!Enum.TryParse(request.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                            throw new ArgumentException($"Invalid fees paid by: {request.FeesPaidBy}");

                        eventEntity.fees_paid_by = feesPaidBy;
                    }

                    var companyFeeApplied = request.CompanyFeeApplied?.ToLower() == "yes";

                    if (!string.IsNullOrEmpty(request.CompanyFeeApplied))
                        eventEntity.company_fee_applied = companyFeeApplied;

                    if (request.CompanyFeeAmount.HasValue)
                        eventEntity.company_fee_amount = request.CompanyFeeAmount.Value;

                    if (request.CompanyFeeStatementDate.HasValue)
                        eventEntity.company_fee_statement_date = request.CompanyFeeStatementDate;

                    await _performanceRepository.UpdateAsync(eventEntity);


                    //
                    var docInspection = await _docInspectionRepository.GetDocInspectionByEventIdAsync(eventEntity.event_id);

                    if (!string.IsNullOrEmpty(request.Status))
                    {
                        if (!Enum.TryParse(request.Status, true, out DocInspectionStatus status))
                            throw new ArgumentException($"Invalid status by: {request.Status}");

                        docInspection.status = status;
                    }

                    if (request.InspectionLevel.HasValue)
                    {
                        if (!Enum.IsDefined(typeof(InspectionLevel), request.InspectionLevel.Value))
                            throw new ArgumentException($"Invalid doc inspection level: {request.InspectionLevel.Value}");

                        int inspectionLevel = request.InspectionLevel.Value;

                        docInspection.inspection_level = inspectionLevel;
                    }

                    if (!string.IsNullOrEmpty(request.Citation))
                    {
                        if (!Enum.TryParse(request.Citation, true, out CitationStatus citation))
                            throw new ArgumentException($"Invalid citation by: {request.Citation}");

                        docInspection.citation = citation;
                    }

                    await _docInspectionRepository.UpdateAsync(docInspection);

                    int docInspectionId = docInspection.doc_inspection_id;

                    // Handle Doc Inspection Document
                    if (request.DocInspectionDoc != null)
                    {
                        bool docInpectionDocExist = await _docInspectionRepository.DocInspectionDocumentExist(docInspectionId);
                        if (request.DocNumber == null)
                            return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse("Doc Inspection Doc Number required", 400);

                        await _docInspectionRepository.UpdateDotInspectionDocsAsync(companyId, eventEntity.event_id, docInspectionId, request.DocInspectionDoc, request.DocNumber);
                    }

                    // Handle Violations
                    if (request.Violations != null && request.Violations.Any())
                    {
                        await _violationRepository.UpdateDotInspectionViolationsAsync(eventEntity.event_id, request.Violations);
                    }

                    await transaction.CommitAsync();

                    return ServiceResponse<DotInspectionUpdateResult>.SuccessResponse(null, "Dot inspection updated successfully.");
                }
                catch (ArgumentException ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(ex, "Validation error while updating DOT inspection.");
                    return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse(ex.Message, 400);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error updating dot inspection");
                    return ServiceResponse<DotInspectionUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }
    }
}
