using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class IssueTicketService : IIssueTicketService
    {
        private readonly IIssueTicketRepository _issueTicketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly ILogger<IssueTicketService> _logger;

        public IssueTicketService(
            IIssueTicketRepository issueTicketRepository,
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            ICarrierRepository carrierRepository,
            ILogger<IssueTicketService> logger)
        {
            _issueTicketRepository = issueTicketRepository;
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _carrierRepository = carrierRepository;
            _logger = logger;
        }

        //public async Task<ServiceResponse<IssueTicketResult>> CreateIssueTicketAsync(IssueTicketRequest request)
        //{
        //    // Validate that the issue exists
        //    var issue = await _maintenanceIssueRepository.GetByIdAsync(request.IssueId);
        //    if (issue == null)
        //        return ServiceResponse<IssueTicketResult>.ErrorResponse("Maintenance issue not found.");

        //    // Validate if the user exists and belongs to the given company
        //    var user = await _userRepository.GetByIdAsync(request.AssignedUserId);
        //    if (user == null || user.company_id != request.CarrierId)
        //        return ServiceResponse<IssueTicketResult>.ErrorResponse("User does not exist or does not belong to the given company.");

        //    // Validate issue type for recurring management
        //    if (request.IsManagedRecurringly && issue.issue_type != IssueType.recurring)
        //        return ServiceResponse<IssueTicketResult>.ErrorResponse("This issue cannot be managed recurringly.");

        //    //// Calculate next schedule date for recurring issues
        //    //DateTime? nextScheduleDate = null;
        //    //if (issue.issue_type == IssueType.recurring && issue.schedule_interval.HasValue)
        //    //{
        //    //    nextScheduleDate = request.ScheduleDate.Add(issue.schedule_interval.Value - request.ScheduleDate);

        //    //    // Assuming schedule_interval is a number representing days
        //    //    // int? daysInterval = issue.schedule_interval.Value.Day; // or another field representing the interval in days
        //    //    // if (daysInterval.HasValue)
        //    //    // {
        //    //    //     nextScheduleDate = request.ScheduleDate.AddDays(daysInterval.Value);
        //    //    // }
        //    //}

        //    try
        //    {
        //        // Create the issue ticket
        //        var issueTicket = new IssueTicket
        //        {
        //            //issue_id = request.IssueId,
        //            assigned_user = request.AssignedUserId,
        //            carrier_id = request.CarrierId,
        //            ismanaged_recurringly = request.IsManagedRecurringly,
        //            priority = Enum.Parse<Priority>(request.Priority),
        //            schedule_date = request.ScheduleDate,
        //            //next_schedule_date = nextScheduleDate,
        //            completed_date = null,
        //            status = IssueTicketStatus.open
        //        };

        //        await _issueTicketRepository.AddAsync(issueTicket);
        //        return ServiceResponse<IssueTicketResult>.SuccessResponse(new IssueTicketResult
        //        {
        //            Success = true
        //        });

        //    }
        //    catch (Exception ex)
        //    {
        //        // Consider logging the exception here for diagnostic purposes
        //        return ServiceResponse<IssueTicketResult>.ErrorResponse("An error occurred while creating the issue ticket. Please try again later.");
        //    }
        //}

        //public async Task<ServiceResponse<IEnumerable<IssueTicketDto>>> GetIssueTicketsByCompanyIdAsync(int companyId)
        //{
        //    try
        //    {
        //        // Fetch issue tickets from repository
        //        var tickets = await _issueTicketRepository.GetTicketsByCompanyIdAsync(companyId);

        //        if (tickets == null || !tickets.Any())
        //            return ServiceResponse<IEnumerable<IssueTicketDto>>.ErrorResponse("No issue tickets found for the specified company.");

        //        // Map to DTOs (or return raw data if no mapping is necessary)
        //        var ticketDtos = tickets.Select(ticket => new IssueTicketDto
        //        {
        //            TicketId = ticket.ticket_id,
        //            //IssueId = ticket.issue_id,
        //            ScheduleDate = ticket.schedule_date,
        //            NextScheduleDate = ticket.next_schedule_date,
        //            AssignedUserId = ticket.assigned_user,
        //            Status = ticket.status.ToString(),
        //            Priority = ticket.priority.ToString()
        //        }).ToList();

        //        return ServiceResponse<IEnumerable<IssueTicketDto>>.SuccessResponse(ticketDtos);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ServiceResponse<IEnumerable<IssueTicketDto>>.ErrorResponse("An internal error occurred. Please try again later.");
        //    }
        //}

        public async Task<ServiceResponse<List<IssueTicketResponseDto>>> GetIssueTicketsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching issue tickets for company ID: {CompanyId}", companyId);

                var tickets = await _issueTicketRepository.GetIssueTicketsAsync(companyId);

                if (tickets == null || !tickets.Any())
                {
                    _logger.LogWarning("No issue tickets found for company ID: {CompanyId}", companyId);
                    return ServiceResponse<List<IssueTicketResponseDto>>.ErrorResponse("No issue tickets found.", 200);
                }
                return ServiceResponse<List<IssueTicketResponseDto>>.SuccessResponse(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching issue tickets for company ID: {CompanyId}", companyId);
                return ServiceResponse<List<IssueTicketResponseDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        //public async Task<ServiceResponse<IssueTicketResult>> CreateIssueTicketAsync(CreateIssueTicketRequest request, int companyId, int userId)
        //{
        //    try
        //    {
        //        int? carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);
        //        if (carrierId == null)
        //            return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Invalid company." };

        //        // Validation: Ensure Vehicle_ID, Assigned_User, and Category_ID belong to the same company
        //        if (!await _issueTicketRepository.ValidateCompanyEntities(request.VehicleId, request.AssignedUser, request.CatagoryId, carrierId.Value))
        //            return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Invalid vehicle, user, or category for this company." };

        //        // Schedule Date Validation: At least 24 hours in the future
        //        if (request.ScheduleDate < DateTime.UtcNow.AddHours(24))
        //            return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Schedule date must be at least 24 hours in the future." };

        //        // Recurrence Rule
        //        DateTime? nextScheduleDate = null;
        //        int? nextMileage = null;

        //        if (request.IsManagedRecurringly)
        //        {
        //            if (request.RecurrentType == "time")
        //                nextScheduleDate = request.ScheduleDate.AddDays((double)request.TimeInterval);
        //            else if (request.RecurrentType == "mileage")
        //                nextMileage = request.CurrentMileage + request.MileageInterval;
        //        }

        //        var issueTicket = new IssueTicket
        //        {
        //            category_id = request.CatagoryId,
        //            vehicle_id = request.VehicleId,
        //            schedule_date = request.ScheduleDate,
        //            next_schedule_date = nextScheduleDate,
        //            priority = Enum.TryParse<Priority>(request.Priority, true, out var priority) ? priority : throw new Exception("Invalid Priority"),
        //            status = IssueTicketStatus.open,
        //            assigned_user = request.AssignedUser,
        //            ismanaged_recurringly = request.IsManagedRecurringly,
        //            recurrent_type = Enum.TryParse<RecurrentType>(request.RecurrentType, true, out var RecurrentType) ? RecurrentType : throw new Exception("Invalid RecurrentType"),
        //            time_interval = request.TimeInterval,
        //            mileage_interval = request.MileageInterval,
        //            current_mileage = request.CurrentMileage,
        //            next_mileage = nextMileage,
        //            created_by = userId,
        //            carrier_id = carrierId.Value,
        //            updated_at = DateTime.UtcNow,
        //            completed_date = null,
        //            company_id = companyId
        //        };

        //        int ticketId = await _issueTicketRepository.CreateIssueTicketAsync(issueTicket);

        //        // Handle File Uploads
        //        if (request.Image != null && request.Image.Any())
        //        {
        //            var filePaths = await _issueTicketRepository.SaveTicketFilesAsync(ticketId, request.Image);
        //            if (filePaths == null)
        //                return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Failed to save files." };
        //        }

        //        return new ServiceResponse<IssueTicketResult> { Success = true, Message = "Issue ticket created successfully." };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = $"An error occurred: {ex.Message}" };
        //    }
        //}
        public async Task<ServiceResponse<IssueTicketResult>> CreateIssueTicketAsync(CreateIssueTicketRequest request, int companyId, int userId)
        {
            try
            {
                _logger.LogInformation("Creating issue ticket for company ID: {CompanyId}, User ID: {UserId}", companyId, userId);

                int? carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);
                if (carrierId == null)
                {
                    _logger.LogWarning("Invalid company ID: {CompanyId}", companyId);
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Invalid company.", 400);
                }

                // Validate image format
                if (request.Image?.Any() == true)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                    foreach (var file in request.Image)
                    {
                        var contentType = file.ContentType?.ToLowerInvariant();

                        if (!allowedTypes.Contains(contentType))
                            return ServiceResponse<IssueTicketResult>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                    }
                }

                // Validate individual entities and return appropriate message
                var companyValidation = await _issueTicketRepository.ValidateCompanyEntitiesDetailed(request.VehicleId, request.AssignedUser, request.CatagoryId, carrierId.Value);

                if (!companyValidation.UserValid)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("No User found for the provided User_ID", 400);

                if (!companyValidation.CategoryValid)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("No Category found for the provided Category_ID.", 400);

                if (!companyValidation.VehicleValid)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("No vehicle found for the provided Vehicle ID.", 400);

                if (request.ScheduleDate < DateTime.UtcNow.AddHours(24))
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Schedule date must be at least 24 hours in the future.", 400);

                //// Validate Vehicle, User, and Category belong to the same company
                //if (!await _issueTicketRepository.ValidateCompanyEntities(request.VehicleId, request.AssignedUser, request.CatagoryId, carrierId.Value))
                //{
                //    _logger.LogWarning("Invalid vehicle, user, or category for company ID: {CompanyId}", companyId);
                //    return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Invalid vehicle, user, or category for this company." };
                //}

                // Schedule Date Validation: At least 24 hours in the future
                //if (request.ScheduleDate < DateTime.UtcNow.AddHours(24))
                //{
                //    _logger.LogWarning("Invalid schedule date: {ScheduleDate}", request.ScheduleDate);
                //    return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Schedule date must be at least 24 hours in the future." };
                //}

                // Initialize nullable values                
                DateTime? nextScheduleDate = null;
                int? nextMileage = null, timeInterval = null, mileageInterval = null, currentMileage = null;
                RecurrentType? recurrentType = null;

                if (request.IsManagedRecurringly)
                {
                    // Ensure RecurrentType is valid
                    if (!Enum.TryParse<RecurrentType>(request.RecurrentType, true, out var parsedRecurrentType))
                        return ServiceResponse<IssueTicketResult>.ErrorResponse("Invalid RecurrentType.", 400);

                    recurrentType = parsedRecurrentType;

                    if (recurrentType == RecurrentType.time)
                    {
                        timeInterval = request.TimeInterval;

                        if (!timeInterval.HasValue)
                            return ServiceResponse<IssueTicketResult>.ErrorResponse("Time interval is required when recurrent type is set to 'time'", 400);

                        nextScheduleDate = request.ScheduleDate.AddDays(timeInterval.Value);
                    }
                    else if (recurrentType == RecurrentType.mileage)
                    {
                        mileageInterval = request.MileageInterval;
                        currentMileage = request.CurrentMileage;
                        
                        if (!currentMileage.HasValue || !mileageInterval.HasValue)
                            return ServiceResponse<IssueTicketResult>.ErrorResponse("Current mileage and mileage interval are required when recurrent type is set to 'mileage'", 400);

                        nextMileage = currentMileage.Value + mileageInterval.Value;
                    }
                }

                var issueTicket = new IssueTicket
                {
                    category_id = request.CatagoryId,
                    vehicle_id = request.VehicleId,
                    schedule_date = request.ScheduleDate,
                    next_schedule_date = nextScheduleDate,
                    priority = Enum.TryParse<Priority>(request.Priority, true, out var priority) ? priority : throw new Exception("Invalid Priority"),
                    status = IssueTicketStatus.open,
                    assigned_user = request.AssignedUser,
                    ismanaged_recurringly = request.IsManagedRecurringly,
                    recurrent_type = recurrentType,
                    time_interval = timeInterval,
                    mileage_interval = mileageInterval,
                    current_mileage = currentMileage,
                    next_mileage = nextMileage,
                    created_by = userId,
                    carrier_id = carrierId.Value,
                    updated_at = DateTime.UtcNow,
                    completed_date = null,
                    company_id = companyId
                };

                int ticketId = await _issueTicketRepository.CreateIssueTicketAsync(issueTicket);
                _logger.LogInformation("Issue ticket created successfully with ID: {TicketId}", ticketId);

                // Handle File Uploads (Only if images exist)
                if (request.Image?.Any() == true)
                {
                    var filePaths = await _issueTicketRepository.SaveTicketFilesAsync(ticketId, request.Image, companyId, issueTicket.vehicle_id);
                    if (filePaths == null)
                        return ServiceResponse<IssueTicketResult>.ErrorResponse("Failed to save files.", 503);
                }

                return ServiceResponse<IssueTicketResult>.SuccessResponse(null, "Issue ticket created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error in CreateIssueTicketAsync");
                return ServiceResponse<IssueTicketResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }


        public async Task<ServiceResponse<IssueTicketResult>> UpdateIssueTicketAsync(UpdateIssueTicketRequest request, int userId, int companyId)
        {
            try
            {
                _logger.LogInformation("Updating issue ticket ID: {TicketId} for company ID: {CompanyId}", request.TicketId, companyId);

                var ticket = await _issueTicketRepository.GetByIdAsync(request.TicketId);
                if (ticket == null)
                {
                    _logger.LogWarning("Issue ticket not found. ID: {TicketId}", request.TicketId);
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Invalid Ticket_ID.", 400);
                }

                if (ticket.company_id != companyId)
                {
                    _logger.LogWarning("Unauthorized attempt to update ticket ID: {TicketId}", request.TicketId);
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("You do not have permission to access this resource", 401);
                }

                //int companyId = await _issueTicketRepository.GetCompanyIdByTicketIdAsync(request.TicketId);
                //if (companyId <= 0)
                //    return new ServiceResponse<IssueTicketResult> { Success = false, Message = "Unable to verify company ownership." };

                int? carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);
                if (carrierId == null)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);

                // Validation: Ensure Vehicle_ID, Assigned_User, and Category_ID belong to the same company
                int? vehicleId = request.VehicleId;
                int? assignedUser = request.AssignedUser;
                int? categoryId = request.CategoryId;

                //if ((vehicleId.HasValue || assignedUser.HasValue || categoryId.HasValue) &&
                //    !await _issueTicketRepository.ValidateCompanyEntitiesToUpdateIssueTicket(vehicleId ?? 0, assignedUser ?? 0, categoryId ?? 0, carrierId.Value))
                //{
                //    return new ServiceResponse<IssueTicketResult> { Success = false, ErrorMessage = "Invalid vehicle, user, or category for this company." };
                //}

                var (vehicleValid, userValid, categoryValid) = await _issueTicketRepository.ValidateCompanyEntitiesToUpdateIssueTicket(request.VehicleId, request.AssignedUser, request.CategoryId, carrierId.Value);

                if (!userValid)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("No User found for the provided User_ID", 400);

                if (!categoryValid)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("No Category found for the provided Category_ID.", 400);

                if (!vehicleValid)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("No vehicle found for the provided Vehicle ID.", 400);

                //if (!vehicleValid || !userValid || !categoryValid)
                //{
                //    var errorMessages = new List<string>();

                //    if (!vehicleValid)
                //        errorMessages.Add("Invalid vehicle for this company.");

                //    if (!userValid)
                //        errorMessages.Add("Invalid user for this company.");

                //    if (!categoryValid)
                //        errorMessages.Add("Invalid category for this company.");

                //    return new ServiceResponse<IssueTicketResult>
                //    {
                //        Success = false,
                //        ErrorMessage = string.Join(" ", errorMessages) // Combine error messages
                //    };
                //}


                // Apply updates only to fields provided in the request
                if (request.CategoryId > 0)
                    ticket.category_id = request.CategoryId;

                if (request.VehicleId > 0)
                    ticket.vehicle_id = request.VehicleId;
                
                if (request.ScheduleDate.HasValue)
                {
                    if (request.ScheduleDate < DateTime.UtcNow.AddHours(24))
                    {
                        return ServiceResponse<IssueTicketResult>.ErrorResponse("Schedule date must be at least 24 hours in the future.", 400);
                    }
                    ticket.schedule_date = request.ScheduleDate.Value;
                }

                if ((Enum.TryParse<Priority>(request.Priority, true, out var priority)))
                    ticket.priority = priority;

                if (!string.IsNullOrEmpty(request.Status))
                {
                    if (request.Status.ToLower() == "resolved")
                    {
                        ticket.completed_date = DateTime.UtcNow;
                    }
                    else if (ticket.status == IssueTicketStatus.resolved && request.Status != "resolved")
                    {
                        ticket.completed_date = null;
                    }
                    ticket.status = Enum.TryParse<IssueTicketStatus>(request.Status, true, out var status) ? status : throw new Exception("Invalid IssueTicketStatus");
                }

                if (request.AssignedUser > 0)
                    ticket.assigned_user = request.AssignedUser;

                if (ticket.ismanaged_recurringly == false && request.IsManagedRecurringly == true && string.IsNullOrEmpty(request.RecurrentType))
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Please select a Recurring Type before enabling recurring management.", 400);

                if (request.RecurrentType?.ToLower() == "time" && !request.TimeInterval.HasValue)
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Time interval is required when recurrent type is set to 'time'", 400);

                if (request.RecurrentType?.ToLower() == "mileage" && (!request.CurrentMileage.HasValue || !request.MileageInterval.HasValue))
                    return ServiceResponse<IssueTicketResult>.ErrorResponse("Current mileage and mileage interval are required when recurrent type is set to 'mileage'", 400);

                if (request.IsManagedRecurringly.HasValue)
                    ticket.ismanaged_recurringly = request.IsManagedRecurringly.Value;

                // Handle recurrence logic
                if (request.IsManagedRecurringly == false)
                {
                    ticket.recurrent_type = null;
                    ticket.time_interval = null;
                    ticket.mileage_interval = null;
                    ticket.next_mileage = null;
                    ticket.next_schedule_date = null;
                }
                else if (request.IsManagedRecurringly == true)
                {
                    if (!string.IsNullOrEmpty(request.RecurrentType))
                        ticket.recurrent_type = Enum.TryParse<RecurrentType>(request.RecurrentType, true, out var recurrentType) ? recurrentType : throw new Exception("Invalid RecurrentType");

                    // If Recurrent Type is "Time"
                    if (ticket.recurrent_type == RecurrentType.time)
                    {
                        if (request.TimeInterval.HasValue)
                        {
                            ticket.time_interval = request.TimeInterval.Value;

                            if (request.ScheduleDate.HasValue)
                            {
                                ticket.next_schedule_date = request.ScheduleDate.Value.AddDays(request.TimeInterval.Value);
                                ticket.schedule_date = request.ScheduleDate;
                            }
                            else
                            {
                                ticket.next_schedule_date = ticket.schedule_date.Value.AddDays(request.TimeInterval.Value);
                            }
                        }
                    }

                    // If Recurrent Type is "Mileage"
                    if (ticket.recurrent_type == RecurrentType.mileage)
                    {
                        if (request.MileageInterval.HasValue)
                        {
                            ticket.mileage_interval = request.MileageInterval.Value;

                            if (request.CurrentMileage.HasValue)
                            {
                                ticket.current_mileage = request.CurrentMileage.Value;
                                ticket.next_mileage = request.CurrentMileage.Value + request.MileageInterval.Value;
                            }
                            else if (ticket.current_mileage.HasValue)
                            {
                                ticket.next_mileage = ticket.current_mileage.Value + request.MileageInterval.Value;
                            }
                        }
                    }
                }

                ticket.updated_at = DateTime.UtcNow;

                if (request.Image != null && request.Image.Any())
                {
                    foreach (var file in request.Image)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLower();
                        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
                            return ServiceResponse<IssueTicketResult>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                    }
                }

                await _issueTicketRepository.UpdateAsync(ticket);

                // File handling
                if (request.Image != null && request.Image.Any())
                {
                    var filePaths = await _issueTicketRepository.SaveTicketFilesAsync(request.TicketId, request.Image, companyId, vehicleId);
                    if (filePaths == null)
                        return ServiceResponse<IssueTicketResult>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                }               

                _logger.LogInformation("Issue ticket updated successfully. ID: {TicketId}", request.TicketId);
                return ServiceResponse<IssueTicketResult>.SuccessResponse(null, "Issue ticket updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating issue ticket");
                return ServiceResponse<IssueTicketResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}