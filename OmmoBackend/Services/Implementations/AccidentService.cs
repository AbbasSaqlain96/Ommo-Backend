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
using Twilio.Http;

namespace OmmoBackend.Services.Implementations
{
    public class AccidentService : IAccidentService
    {
        private readonly IAccidentRepository _accidentRepository;
        private readonly IAccidentPicturesRepository _accidentPicturesRepository;
        private readonly ITruckRepository _truckRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IPerformanceRepository _performanceEventsRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AccidentService> _logger;

        public AccidentService(
            IAccidentRepository accidentRepository,
            IAccidentPicturesRepository accidentPicturesRepository,
            ITruckRepository truckRepository,
            IDriverRepository driverRepository,
            ICarrierRepository carrierRepository,
            IVehicleRepository vehicleRepository,
            ITicketRepository ticketRepository,
            IPerformanceRepository performanceEventsRepository,
            ITrailerRepository trailerRepository,
            IUnitOfWork unitOfWork,
            AppDbContext dbContext,
            ILogger<AccidentService> logger)
        {
            _accidentRepository = accidentRepository;
            _accidentPicturesRepository = accidentPicturesRepository;
            _truckRepository = truckRepository;
            _driverRepository = driverRepository;
            _carrierRepository = carrierRepository;
            _vehicleRepository = vehicleRepository;
            _ticketRepository = ticketRepository;
            _performanceEventsRepository = performanceEventsRepository;
            _trailerRepository = trailerRepository;
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ServiceResponse<AccidentCreationResult>> CreateAccidentAsync(int companyId, CreateAccidentRequest accidentRequest)
        {
            _logger.LogInformation("Starting accident creation for CompanyId: {CompanyId}", companyId);

            if (accidentRequest == null)
            {
                _logger.LogWarning("CreateAccidentRequest is null for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<AccidentCreationResult>.ErrorResponse("Invalid request data.");
            }

            try
            {
                // Validate event date
                if (accidentRequest.EventInfo.EventDate > DateTime.Now)
                {
                    _logger.LogWarning("Event date cannot be in the future for CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Event date cannot be in the future.", 400);
                }

                _logger.LogInformation("Validating driver for accident creation.");

                // Validate Driver
                var driver = await _driverRepository.GetByIdAsync(accidentRequest.EventInfo.DriverId);
                if (driver == null || driver.company_id != companyId)
                {
                    _logger.LogWarning("No driver found or driver belongs to another company. Driver ID: {DriverId}", accidentRequest.EventInfo.DriverId);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("No driver found for the provided Driver ID.", 400);
                }

                _logger.LogInformation("Validating truck for accident creation.");

                // Validate Truck
                var truck = await _truckRepository.GetByIdAsync(accidentRequest.EventInfo.TruckId);
                if (truck == null)
                {
                    _logger.LogWarning("No truck found or truck belongs to another company. Truck ID: {TruckId}", accidentRequest.EventInfo.TruckId);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("No truck found for the provided Truck ID.", 400);
                }

                var vehicle = await _vehicleRepository.GetByIdAsync(truck.vehicle_id);
                if (vehicle == null)
                {
                    _logger.LogWarning("Vehicle ID {VehicleId} does not exist.", truck.vehicle_id);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Vehicle does not exist.");
                }

                if (!vehicle.carrier_id.HasValue)
                {
                    _logger.LogWarning("Vehicle ID {VehicleId} is not associated with a carrier.", truck.vehicle_id);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Vehicle is not associated with a carrier.");
                }

                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null)
                {
                    _logger.LogWarning("Carrier ID {CarrierId} does not exist.", vehicle.carrier_id);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Carrier does not exist.");
                }

                if (carrier.company_id != companyId)
                {
                    _logger.LogWarning("Truck ID {TruckId} belongs to another company.", accidentRequest.EventInfo.TruckId);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Truck belongs to another company.");
                }

                // Truck
                if (accidentRequest.EventInfo.TruckId != 0)
                {
                    var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTruckOwnershipAsync(_truckRepository, accidentRequest.EventInfo.TruckId, companyId);
                    if (!isValid)
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);
                }

                // Driver
                if (accidentRequest.EventInfo.DriverId != 0)
                {
                    var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateDriverOwnershipAsync(_driverRepository, accidentRequest.EventInfo.DriverId, companyId);
                    if (!isValid)
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);
                }

                // Trailer
                if (accidentRequest.EventInfo.TrailerId.HasValue)
                {
                    var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTrailerOwnershipAsync(_trailerRepository, accidentRequest.EventInfo.TrailerId.Value, companyId);
                    if (!isValid)
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);
                }

                // Ticket
                if (accidentRequest.AccidentInfo.TicketId.HasValue)
                {
                    var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTicketOwnershipAsync(_ticketRepository, accidentRequest.AccidentInfo.TicketId.Value, companyId);
                    if (!isValid)
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);
                }

                // Validate document formats
                if (!ValidationHelper.IsValidDocumentFormat(accidentRequest.AccidentDocumentDto.PoliceReportFile, new[] { ".pdf", ".doc", ".docx" }))
                {
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Invalid document format for Police Report. Only PDF, DOC, and DOCX formats are allowed.", 400);
                }

                if (!ValidationHelper.IsValidDocumentFormat(accidentRequest.AccidentDocumentDto.DriverReportFile, new[] { ".pdf", ".doc", ".docx" }))
                {
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Invalid document format for Driver Report. Only PDF, DOC, and DOCX formats are allowed.", 400);
                }

                // Iterate through each image file and check its format
                foreach (var image in accidentRequest.AccidentImageDto.AccidentImages)
                {
                    if (!ValidationHelper.IsValidImageFormat(image, new[] { ".jpeg", ".jpg", ".png", ".webp" }))
                    {
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                    }
                }

                _logger.LogInformation("Validating ticket ID for Accident creation.");

                // Validate Ticket ID Ownership
                if (accidentRequest.AccidentInfo.TicketId.HasValue)
                {
                    var ticket = await _ticketRepository.GetByIdAsync(accidentRequest.AccidentInfo.TicketId.Value);
                    if (ticket == null)
                    {
                        _logger.LogWarning("Ticket ID {TicketId} does not exist.", accidentRequest.AccidentInfo.TicketId);
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse("Ticket does not exist.");
                    }

                    // Retrieve the event associated with the ticket
                    var eventInfo = await _performanceEventsRepository.GetByIdAsync(ticket.event_id);
                    if (eventInfo == null)
                    {
                        _logger.LogWarning("Event associated with Ticket ID {TicketId} does not exist.", ticket.ticket_id);
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse("Event associated with Ticket ID does not exist.");
                    }

                    // Check if the ticket's company matches the authenticated company
                    if (eventInfo.company_id != companyId)
                    {
                        _logger.LogWarning("Ticket ID {TicketId} does not belong to CompanyId: {CompanyId}", ticket.ticket_id, companyId);
                        return ServiceResponse<AccidentCreationResult>.ErrorResponse("Ticket ID does not belong to your company.");
                    }
                }

                _logger.LogInformation("Creating Performance Event for Accident.");

                if (!Enum.TryParse(accidentRequest.EventInfo.Authority, true, out EventAuthority eventAuthority))
                    throw new ArgumentException($"Invalid event authority: {accidentRequest.EventInfo.Authority}");

                if (!Enum.TryParse(accidentRequest.EventInfo.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                    throw new ArgumentException($"Invalid fees paid by: {accidentRequest.EventInfo.FeesPaidBy}");

                var companyFeeApplied = accidentRequest.EventInfo.CompanyFeeApplied?.ToLower() == "yes";

                // Create Performance Event
                var performanceEvent = new PerformanceEvents
                {
                    truck_id = accidentRequest.EventInfo.TruckId,
                    driver_id = accidentRequest.EventInfo.DriverId,
                    trailer_id = accidentRequest.EventInfo.TrailerId ?? 0,
                    location = accidentRequest.EventInfo.Location,
                    event_type = EventType.accident,
                    authority = eventAuthority,
                    date = accidentRequest.EventInfo.EventDate,
                    description = accidentRequest.EventInfo.Description,
                    load_id = accidentRequest.EventInfo.LoadId ?? 0,
                    event_fees = accidentRequest.EventInfo.EventFee,
                    fees_paid_by = feesPaidBy,
                    company_fee_applied = companyFeeApplied,
                    company_fee_amount = accidentRequest.EventInfo.CompanyFeeAmount,
                    company_fee_statement_date = accidentRequest.EventInfo.CompanyFeeStatementDate,
                    company_id = companyId
                };

                _logger.LogInformation("Creating accident and related data within a transaction.");

                // Create Accident & Related Data in a Transaction
                bool isCreated = await _accidentRepository.CreateAccidentWithTransactionAsync(
                    performanceEvent, accidentRequest.AccidentInfo, accidentRequest.AccidentDocumentDto, accidentRequest.AccidentImageDto, ClaimParser.DeserializeClaims(accidentRequest.ClaimInfoJson), companyId);

                if (!isCreated)
                {
                    _logger.LogError("Failed to create accident for CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<AccidentCreationResult>.ErrorResponse("Failed to create the Accident.", 503);
                }

                _logger.LogInformation("Successfully created accident for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<AccidentCreationResult>.SuccessResponse(null, "Accident created successfully.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating an accident.");
                return ServiceResponse<AccidentCreationResult>.ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accident for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<AccidentCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }


        public async Task<ServiceResponse<AccidentUpdateResult>> UpdateAccidentAsync(UpdateAccidentRequest request, int companyId)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var performanceEvent = await _performanceEventsRepository.GetByIdAsync(request.updateAccidentEventInfoDto.EventId);
                    if (performanceEvent == null)
                        return ServiceResponse<AccidentUpdateResult>.ErrorResponse("No accident found for the provided Event ID.", 400);

                    if (performanceEvent.company_id != companyId)
                        return ServiceResponse<AccidentUpdateResult>.ErrorResponse("You do not have permission to access this resource", 401);

                    if (performanceEvent.event_type != EventType.accident)
                        return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Accident event not found or invalid type.", 404);

                    // Document Format Validation
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                    // Validate Driver Report File
                    if (request.updateAccidentDocumentDto.DriverReportFile != null)
                    {
                        var driverReportFileName = request.updateAccidentDocumentDto.DriverReportFile.FileName?.Trim();

                        if (!string.IsNullOrEmpty(driverReportFileName))
                        {
                            var driverReportFileNameExtension = Path.GetExtension(driverReportFileName).ToLowerInvariant();

                            if (!allowedExtensions.Contains(driverReportFileNameExtension))
                            {
                                return ServiceResponse<AccidentUpdateResult>.ErrorResponse(
                                    "Invalid Driver Report format. Only PDF, DOC, and DOCX formats are allowed.", 400);
                            }
                        }
                    }

                    // Validate Police Report File
                    if (request.updateAccidentDocumentDto.PoliceReportFile != null)
                    {
                        var policeReportFileName = request.updateAccidentDocumentDto.PoliceReportFile.FileName?.Trim();

                        if (!string.IsNullOrEmpty(policeReportFileName))
                        {
                            var policeReportFileNameExtension = Path.GetExtension(policeReportFileName).ToLowerInvariant();

                            if (!allowedExtensions.Contains(policeReportFileNameExtension))
                            {
                                return ServiceResponse<AccidentUpdateResult>.ErrorResponse(
                                    "Invalid Police Report format. Only PDF, DOC, and DOCX formats are allowed.", 400);
                            }
                        }
                    }

                    // Update performance_event fields
                    var info = request.updateAccidentEventInfoDto;

                    // Truck
                    if (info.TruckId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTruckOwnershipAsync(_truckRepository, info.TruckId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var truck = (Truck)entity!;
                        performanceEvent.truck_id = truck.truck_id;
                    }

                    // Driver
                    if (info.DriverId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateDriverOwnershipAsync(_driverRepository, info.DriverId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var driver = (Driver)entity!;
                        performanceEvent.driver_id = driver.driver_id;
                    }

                    // Trailer
                    if (info.TrailerId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTrailerOwnershipAsync(_trailerRepository, info.TrailerId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var trailer = (Trailer)entity!;
                        performanceEvent.trailer_id = trailer.trailer_id;
                    }

                    if (!string.IsNullOrEmpty(info.Location)) performanceEvent.location = info.Location;

                    if (!string.IsNullOrEmpty(info.Authority))
                    {
                        if (Enum.TryParse<EventAuthority>(info.Authority, ignoreCase: true, out var authority))
                        {
                            performanceEvent.authority = authority;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid authority value: {info.Authority}");
                        }
                    }

                    if (!string.IsNullOrEmpty(info.Description)) performanceEvent.description = info.Description;

                    if (info.LoadId.HasValue) performanceEvent.load_id = info.LoadId.Value;

                    if (info.EventFee.HasValue) performanceEvent.event_fees = info.EventFee.Value;

                    if (!string.IsNullOrEmpty(info.FeesPaidBy))
                    {
                        if (Enum.TryParse<FeesPaidBy>(info.FeesPaidBy, ignoreCase: true, out var feesPaidBy))
                        {
                            performanceEvent.fees_paid_by = feesPaidBy;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid fees paid by value: {info.FeesPaidBy}");
                        }
                    }


                    if (!string.IsNullOrEmpty(info.CompanyFeeApplied))
                        performanceEvent.company_fee_applied = info.CompanyFeeApplied.Equals("Yes", StringComparison.OrdinalIgnoreCase);

                    if (info.CompanyFeeAmount.HasValue) performanceEvent.company_fee_amount = info.CompanyFeeAmount.Value;

                    if (info.CompanyFeeStatementDate.HasValue) performanceEvent.company_fee_statement_date = info.CompanyFeeStatementDate.Value;

                    if (info.EventDate.HasValue)
                    {
                        if (info.EventDate.Value > DateTime.UtcNow)
                        {
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Event date cannot be in the future.", 400);
                        }

                        performanceEvent.date = info.EventDate.Value;
                    }

                    // Update event
                    await _performanceEventsRepository.UpdateAsync(performanceEvent);

                    var accident = await _accidentRepository.GetAccidentByEventIdAsync(info.EventId);
                    if (accident == null)
                        return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Accident not found for the given Event ID.", 404);

                    // Update accident fields
                    var accInfo = request.updateAccidentInfoDto;

                    // Ticket
                    if (request.updateAccidentInfoDto.TicketId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTicketOwnershipAsync(_ticketRepository, request.updateAccidentInfoDto.TicketId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var ticket = (UnitTicket)entity!;
                        accInfo.TicketId = ticket.ticket_id;
                    }

                    if ((bool)accInfo.DriverFault.HasValue) accident.driver_fault = (bool)accInfo.DriverFault;

                    if ((bool)accInfo.AlcoholTest.HasValue) accident.alcohol_test = (bool)accInfo.AlcoholTest;

                    if (accInfo.DrugTestDateTime.HasValue) accident.drug_test_date_time = accInfo.DrugTestDateTime;

                    if (accInfo.AlcoholTestDateTime.HasValue) accident.alcohol_test_date_time = accInfo.AlcoholTestDateTime;

                    if ((bool)accInfo.HasCasualties.HasValue) accident.has_casuality = (bool)accInfo.HasCasualties;

                    if ((bool)accInfo.DriverDrugTested.HasValue) accident.driver_drug_test = (bool)accInfo.DriverDrugTested;

                    if (accInfo.TicketId.HasValue) accident.ticket_id = accInfo.TicketId;

                    // Update accident
                    await _accidentRepository.UpdateAsync(accident);

                    // Validate file formats before calling update methods
                    var allowedDocExtensions = new[] { ".pdf", ".doc", ".docx" };
                    var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };

                    if (request.updateAccidentDocumentDto?.PoliceReportFile != null)
                    {
                        var ext = Path.GetExtension(request.updateAccidentDocumentDto.PoliceReportFile.FileName).ToLower();
                        if (!allowedDocExtensions.Contains(ext))
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Format of Police Report is invalid.", 400);
                    }

                    if (request.updateAccidentDocumentDto?.DriverReportFile != null)
                    {
                        var ext = Path.GetExtension(request.updateAccidentDocumentDto.DriverReportFile.FileName).ToLower();
                        if (!allowedDocExtensions.Contains(ext))
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Format of Driver Report is invalid.", 400);
                    }

                    if (request.updateAccidentImageDto?.AccidentImages != null)
                    {
                        foreach (var img in request.updateAccidentImageDto.AccidentImages)
                        {
                            var ext = Path.GetExtension(img.FileName).ToLower();
                            if (!allowedImageExtensions.Contains(ext))
                                return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Format of Accident Image is invalid.", 400);
                        }
                    }

                    // Sync documents (Police & Driver Reports)
                    if (request.updateAccidentDocumentDto != null)
                    {
                        bool policeDocExist = await _accidentRepository.PoliceReportDocumentExist(accident.accident_id);
                        if (!policeDocExist && request.updateAccidentDocumentDto.PoliceReportNumber == null)
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Police Report Number required", 400);

                        bool driverDocExist = await _accidentRepository.DriverReportDocumentExist(accident.accident_id);
                        if (!driverDocExist && request.updateAccidentDocumentDto.DriverReportNumber == null)
                            return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Driver Report Number required", 400);

                        await _accidentRepository.UpdateDocumentsAsync(accident.accident_id, companyId, request.updateAccidentDocumentDto);
                    }

                    // Fetch current paths from DB
                    var currentImages = await _accidentPicturesRepository.GetAccidentImagesByAccidentIdAsync(accident.accident_id);

                    var existingPaths = currentImages.Select(i => i.picture_url).ToList();

                    // Sync images
                    if (request.updateAccidentImageDto?.AccidentImages != null)
                    {
                        await _accidentRepository.SyncAccidentImagesAsync(
                            accident.accident_id,
                            accident.event_id,
                            companyId,
                            request.updateAccidentImageDto.AccidentImages ?? new List<IFormFile>(),
                            existingPaths
                        );
                    }

                    // Sync claims
                    if (request.ClaimInfoJson != null)
                    {
                        await _accidentRepository.SyncClaimsAsync(accident.event_id, ClaimParser.DeserializeClaims(request.ClaimInfoJson));
                    }

                    await transaction.CommitAsync();

                    return ServiceResponse<AccidentUpdateResult>.SuccessResponse(null, "Accident updated successfully.");
                }
                catch (ArgumentException ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(ex, "Validation error while updating accident.");
                    return ServiceResponse<AccidentUpdateResult>.ErrorResponse(ex.Message, 400);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error updating accident");
                    return ServiceResponse<AccidentUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }
    }
}
