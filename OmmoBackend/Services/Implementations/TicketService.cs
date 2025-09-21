using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using Twilio.Http;

namespace OmmoBackend.Services.Implementations
{
    public class TicketService : ITicketService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IViolationRepository _violationRepository;
        private readonly IConfiguration _configuration;
        private readonly ITicketDocRepository _ticketDocRepository;
        private readonly ITruckRepository _truckRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly IPerformanceRepository _performanceRepository;
        private readonly ITicketPictureRepository _ticketPictureRepository;
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TicketService> _logger;

        public TicketService(
            IEventRepository eventRepository,
            ITicketRepository ticketRepository,
            IViolationRepository violationRepository,
            IConfiguration configuration,
            ITicketDocRepository ticketDocRepository,
            ITruckRepository truckRepository,
            IDriverRepository driverRepository,
            ICarrierRepository carrierRepository,
            IVehicleRepository vehicleRepository,
            ITrailerRepository trailerRepository,
            IPerformanceRepository performanceRepository,
            ITicketPictureRepository ticketPictureRepository,
            AppDbContext dbContext,
            IUnitOfWork unitOfWork,
            ILogger<TicketService> logger)
        {
            _eventRepository = eventRepository;
            _ticketRepository = ticketRepository;
            _violationRepository = violationRepository;
            _configuration = configuration;
            _ticketDocRepository = ticketDocRepository;
            _truckRepository = truckRepository;
            _driverRepository = driverRepository;
            _carrierRepository = carrierRepository;
            _vehicleRepository = vehicleRepository;
            _trailerRepository = trailerRepository;
            _performanceRepository = performanceRepository;
            _ticketPictureRepository = ticketPictureRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResponse<TicketDetailResponse>> GetTicketDetailsAsync(int eventId, int companyId)
        {
            _logger.LogInformation("Fetching ticket details for Event ID: {EventId}, Company ID: {CompanyId}", eventId, companyId);

            // Verify if the event belongs to the authenticated user's company
            bool belongsToCompany = await _ticketRepository.EventBelongsToCompany(eventId, companyId);
            if (!belongsToCompany)
            {
                _logger.LogWarning("Event ID: {EventId} does not belong to Company ID: {CompanyId}", eventId, companyId);
                return ServiceResponse<TicketDetailResponse>.ErrorResponse("No ticket info found for the given event ID.", 400);
            }

            var ticketDetails = await _ticketRepository.GetTicketDetailsAsync(eventId, companyId);
            if (ticketDetails == null)
            {
                _logger.LogWarning("No ticket details found for Event ID: {EventId}, Company ID: {CompanyId}", eventId, companyId);
                return ServiceResponse<TicketDetailResponse>.ErrorResponse("No ticket details found for the given event ID.", 400);
            }

            _logger.LogInformation("Successfully fetched ticket details for Event ID: {EventId}, Company ID: {CompanyId}", eventId, companyId);
            return ServiceResponse<TicketDetailResponse>.SuccessResponse(new TicketDetailResponse { TicketDetails = ticketDetails });
        }

        public async Task<ServiceResponse<TicketCreationResult>> CreateTicketAsync(
            int companyId, CreateTicketRequest ticketRequest)
        {
            try
            {
                _logger.LogInformation("Creating a new ticket for Company ID: {CompanyId}", companyId);

                // Event Date Validation
                if (ticketRequest.EventInfo.EventDate.Date > DateTime.UtcNow.Date)
                    return ServiceResponse<TicketCreationResult>.ErrorResponse("Event date cannot be in the future.", 400);

                // Ticket Status Validation
                if (!string.IsNullOrEmpty(ticketRequest.TicketInfo.Status))
                {
                    if (!TicketValidationHelper.IsValidStatus(ticketRequest.TicketInfo.Status))
                    {
                        return ServiceResponse<TicketCreationResult>.ErrorResponse(
                            $"Invalid ticket status. Allowed values are: {TicketValidationHelper.GetAllowedStatusesString()}.", 400);
                    }
                }

                // Document Null Check
                if (ticketRequest.TicketDocument.Document == null)
                    return ServiceResponse<TicketCreationResult>.ErrorResponse("Document is required.", 400);

                // Document Format Validation
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                // Null check for file
                if (ticketRequest.TicketDocument.Document != null)
                {
                    var fileName = ticketRequest.TicketDocument.Document.FileName?.Trim();

                    // Only validate format if fileName is not null or empty
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var extension = Path.GetExtension(fileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            return ServiceResponse<TicketCreationResult>.ErrorResponse("Invalid document format. Only PDF, DOC, and DOCX formats are allowed", 400);
                        }
                    }
                }

                // Truck Validation
                var truck = await _truckRepository.GetByIdAsync(ticketRequest.EventInfo.TruckId);
                if (truck == null)
                {
                    return ServiceResponse<TicketCreationResult>.ErrorResponse(
                        "No Truck found for the provided Truck ID", 400);
                }

                var vehicle = await _vehicleRepository.GetByIdAsync(truck.vehicle_id);
                if (vehicle == null || !vehicle.carrier_id.HasValue)
                {
                    return ServiceResponse<TicketCreationResult>.ErrorResponse("Truck belongs to another company.", 400);
                }

                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null || carrier.company_id != companyId)
                {
                    return ServiceResponse<TicketCreationResult>.ErrorResponse("Truck belongs to another company.", 400);
                }

                // Driver Validation
                var driver = await _driverRepository.GetByIdAsync(ticketRequest.EventInfo.DriverId);
                if (driver == null || driver.company_id != companyId)
                {
                    return ServiceResponse<TicketCreationResult>.ErrorResponse(
                        "No driver found for the provided Driver ID", 400);
                }

                // Validate Trailer if provided
                if (ticketRequest.EventInfo.TrailerId.HasValue)
                {
                    var trailer = await _trailerRepository.GetByIdAsync(ticketRequest.EventInfo.TrailerId.Value);
                    if (trailer == null)
                    {
                        _logger.LogWarning("Trailer {TrailerId} not found", ticketRequest.EventInfo.TrailerId.Value);
                        return ServiceResponse<TicketCreationResult>.ErrorResponse("No trailer found for the provided Trailer ID", 400);
                    }

                    var trailerVehicle = await _vehicleRepository.GetByIdAsync(trailer.vehicle_id);
                    if (trailerVehicle == null || !trailerVehicle.carrier_id.HasValue)
                    {
                        _logger.LogWarning("Invalid vehicle or carrier not found for Trailer {TrailerId}", ticketRequest.EventInfo.TrailerId.Value);
                        return ServiceResponse<TicketCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }

                    var trailerCarrier = await _carrierRepository.GetByIdAsync(trailerVehicle.carrier_id.Value);
                    if (trailerCarrier == null || trailerCarrier.company_id != companyId)
                    {
                        _logger.LogWarning("Carrier mismatch for trailer CompanyId: {CompanyId}", companyId);
                        return ServiceResponse<TicketCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }
                }

                // Violations Validation
                var violationIds = ticketRequest.Violations ?? new List<int>();
                var existingViolations = await _violationRepository.GetByIdsAsync(violationIds);
                if (existingViolations.Count != violationIds.Count)
                {
                    return ServiceResponse<TicketCreationResult>.ErrorResponse("Selected Violations does not exist", 400);
                }

                if (!Enum.TryParse(ticketRequest.EventInfo.Authority, true, out EventAuthority eventAuthority))
                    throw new ArgumentException($"Invalid event authority: {ticketRequest.EventInfo.Authority}");

                // Fees paid by Validation
                if (!Enum.TryParse(ticketRequest.EventInfo.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                    return ServiceResponse<TicketCreationResult>.ErrorResponse(
                        $"Invalid value for fees paid by: {ticketRequest.EventInfo.FeesPaidBy}", 400);

                // Company fee applied Validation
                var feeAppliedValue = ticketRequest.EventInfo.CompanyFeeApplied?.ToLower();
                if (feeAppliedValue != "yes" && feeAppliedValue != "no")
                {
                    return ServiceResponse<TicketCreationResult>.ErrorResponse(
                        "Invalid value for company fee aapplied. Allowed values are yes or no.", 400);
                }
                bool companyFeeApplied = feeAppliedValue == "yes";

                // TicketImages Validation
                if (ticketRequest.TicketImageDto?.TicketImages?.Count > 0)
                {
                    var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    foreach (var image in ticketRequest.TicketImageDto.TicketImages)
                    {
                        var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                        if (!allowedImageExtensions.Contains(ext))
                        {
                            return ServiceResponse<TicketCreationResult>.ErrorResponse(
                                $"Invalid file type {ext} in TicketImages. Allowed: {string.Join(", ", allowedImageExtensions)}", 400);
                        }
                    }
                }

                // Prepare Performance Event Data
                var performanceEvents = new PerformanceEvents
                {
                    event_type = EventType.ticket,
                    driver_id = ticketRequest.EventInfo.DriverId,
                    truck_id = ticketRequest.EventInfo.TruckId,
                    trailer_id = ticketRequest.EventInfo.TrailerId ?? 0,
                    location = ticketRequest.EventInfo.Location,
                    authority = eventAuthority,
                    description = ticketRequest.EventInfo.Description,
                    load_id = ticketRequest.EventInfo.LoadId ?? 0,
                    event_fees = ticketRequest.EventInfo.EventFee ?? 0,
                    fees_paid_by = feesPaidBy,
                    company_fee_applied = companyFeeApplied,
                    company_fee_amount = ticketRequest.EventInfo.CompanyFeeAmount ?? 0,
                    company_fee_statement_date = ticketRequest.EventInfo.CompanyFeeStatementDate,
                    date = ticketRequest.EventInfo.EventDate,
                    company_id = companyId
                };

                // Create Ticket & Related Data in a Transaction
                bool isCreated = await _ticketRepository.CreateTicketWithTransactionAsync(
                    performanceEvents, ticketRequest.TicketInfo, ticketRequest.TicketDocument, ticketRequest.Violations!, ticketRequest.TicketImageDto, companyId);

                if (!isCreated)
                {
                    _logger.LogError("Failed to create ticket for Company ID: {CompanyId}", companyId);
                    return ServiceResponse<TicketCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }

                _logger.LogInformation("Ticket created successfully for Company ID: {CompanyId}", companyId);
                return ServiceResponse<TicketCreationResult>.SuccessResponse(null, "Ticket created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTicketAsync.");
                return ServiceResponse<TicketCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<TicketUpdateResult>> UpdateTicketAsync(
            int companyId, UpdateTicketRequest ticketRequest)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Step 1: Validate ticket ownership
                    var (eventEntity, ticketEntity) = await _ticketRepository.GetTicketByEventIdAsync(ticketRequest.EventId);
                    if (eventEntity == null || ticketEntity == null)
                        return ServiceResponse<TicketUpdateResult>.ErrorResponse("No ticket found for the provided Event ID.", 400);

                    if (eventEntity.company_id != companyId)
                        return ServiceResponse<TicketUpdateResult>.ErrorResponse("Ticket does not belong to your company.", 400);

                    // Truck
                    if (ticketRequest.TruckId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTruckOwnershipAsync(_truckRepository, ticketRequest.TruckId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var truck = (Truck)entity!;
                        eventEntity.truck_id = truck.truck_id;
                    }

                    // Driver
                    if (ticketRequest.DriverId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateDriverOwnershipAsync(_driverRepository, ticketRequest.DriverId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var driver = (Driver)entity!;
                        eventEntity.driver_id = driver.driver_id;
                    }

                    // Trailer
                    if (ticketRequest.TrailerId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTrailerOwnershipAsync(_trailerRepository, ticketRequest.TrailerId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var trailer = (Trailer)entity!;
                        eventEntity.trailer_id = trailer.trailer_id;
                    }

                    if (!string.IsNullOrEmpty(ticketRequest.Location))
                        eventEntity.location = ticketRequest.Location;

                    if (!string.IsNullOrEmpty(ticketRequest.Authority))
                    {
                        if (!Enum.TryParse(ticketRequest.Authority, true, out EventAuthority eventAuthority))
                            throw new ArgumentException($"Invalid event authority: {ticketRequest.Authority}");

                        eventEntity.authority = eventAuthority;
                    }

                    // Event Date Validation
                    if (ticketRequest.EventDate.HasValue)
                    {
                        if (ticketRequest.EventDate.Value > DateTime.UtcNow)
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse("Event date cannot be in the future.", 400);

                        eventEntity.date = ticketRequest.EventDate.Value;
                    }

                    if (!string.IsNullOrEmpty(ticketRequest.Description))
                        eventEntity.description = ticketRequest.Description;

                    if (ticketRequest.LoadId.HasValue)
                        eventEntity.load_id = ticketRequest.LoadId.Value;

                    if (ticketRequest.EventFee.HasValue)
                        eventEntity.event_fees = ticketRequest.EventFee.Value;

                    // Validate FeesPaidBy enum
                    if (!string.IsNullOrEmpty(ticketRequest.FeesPaidBy))
                    {
                        if (!Enum.TryParse(ticketRequest.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse("Invalid value for fees paid by'.", 400);

                        eventEntity.fees_paid_by = feesPaidBy;
                    }

                    // Validate CompanyFeeApplied
                    if (!string.IsNullOrEmpty(ticketRequest.CompanyFeeApplied))
                    {
                        var applied = ticketRequest.CompanyFeeApplied.ToLower();
                        if (applied != "yes" && applied != "no")
                        {
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse("Invalid value for company fee applied. Expected yes or no.", 400);
                        }
                        eventEntity.company_fee_applied = applied == "yes";
                    }

                    if (ticketRequest.CompanyFeeAmount.HasValue)
                        eventEntity.company_fee_amount = ticketRequest.CompanyFeeAmount.Value;

                    if (ticketRequest.CompanyFeeStatementDate.HasValue)
                        eventEntity.company_fee_statement_date = ticketRequest.CompanyFeeStatementDate;

                    await _performanceRepository.UpdateAsync(eventEntity);

                    // Update UnitTicket fields
                    if (!string.IsNullOrEmpty(ticketRequest.Status))
                    {
                        if (!TicketValidationHelper.IsValidStatus(ticketRequest.Status))
                        {
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse(
                                $"Invalid ticket status. Allowed values are: {TicketValidationHelper.GetAllowedStatusesString()}.", 400);
                        }

                        ticketEntity.status = ticketRequest.Status.Trim();
                    }

                    await _ticketRepository.UpdateAsync(ticketEntity);

                    int ticketId = ticketEntity.ticket_id;

                    // Sync document
                    if (ticketRequest.Document != null)
                    {
                        if (!ValidationHelper.IsValidDocumentFormat(ticketRequest.Document, new[] { ".pdf", ".doc", ".docx" }))
                        {
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse("Invalid document format.", 400);
                        }

                        bool ticketDocExist = await _ticketRepository.TicketDocumentExist(ticketId);
                        if (ticketRequest.DocNumber == null)
                            return ServiceResponse<TicketUpdateResult>.ErrorResponse("Ticket Doc Number required", 400);

                        await _ticketDocRepository.UpdateDocsAsync(companyId, ticketEntity.ticket_id, ticketRequest.EventId, ticketRequest.DocNumber, ticketRequest.Document);
                    }

                    // Validate TicketImages file extensions
                    if (ticketRequest.TicketImages != null && ticketRequest.TicketImages.Any())
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                        foreach (var image in ticketRequest.TicketImages)
                        {
                            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(extension))
                            {
                                return ServiceResponse<TicketUpdateResult>.ErrorResponse($"Invalid file type '{extension}' in TicketImages. Allowed: {string.Join(", ", allowedExtensions)}", 400);
                            }
                        }

                        await _ticketPictureRepository.UpdateImagesAsync(companyId, ticketEntity.ticket_id, ticketRequest.EventId, ticketRequest.TicketImages);
                    }

                    // Sync violations
                    if (ticketRequest.Violations != null && ticketRequest.Violations.Any())
                    {
                        await _violationRepository.UpdateTicketViolationsAsync(ticketEntity.ticket_id, ticketRequest.Violations);
                    }

                    _logger.LogInformation("Ticket {TicketId} updated successfully with violations.", ticketEntity.ticket_id);
                    await transaction.CommitAsync();

                    return ServiceResponse<TicketUpdateResult>.SuccessResponse(null, "Ticket updated successfully.");
                }
                catch (ArgumentException ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(ex, "Violation update failed.");
                    return ServiceResponse<TicketUpdateResult>.ErrorResponse(ex.Message, 400);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error updating ticket");
                    return ServiceResponse<TicketUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }
    }
}
