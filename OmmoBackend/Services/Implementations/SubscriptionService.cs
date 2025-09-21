using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IDispatchServiceRepository _dispatcherRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IRequestModuleRepository _requestModuleRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            ICarrierRepository carrierRepository,
            IDispatchServiceRepository dispatcherRepository,
            IModuleRepository moduleRepository,
            IRequestModuleRepository requestModuleRepository,
            ILogger<SubscriptionService> logger
        )
        {
            _subscriptionRepository = subscriptionRepository;
            _carrierRepository = carrierRepository;
            _dispatcherRepository = dispatcherRepository;
            _moduleRepository = moduleRepository;
            _requestModuleRepository = requestModuleRepository;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new subscription request asynchronously based on the provided request data.
        /// </summary>
        /// <param name="createSubscriptionRequest">The request data for creating a subscription request, including dispatcher, carrier, and requested modules.</param>
        /// <exception cref="ArgumentException">Thrown when the dispatcher, carrier, or any module Id is invalid.</exception>
        public async Task CreateSubscriptionRequestAsync(CreateSubscriptionRequest createSubscriptionRequest)
        {
            try
            {
                _logger.LogInformation("Creating subscription request for DispatcherId: {DispatcherId}, CarrierId: {CarrierId}", createSubscriptionRequest.DispatchId, createSubscriptionRequest.CarrierId);

                // Retrieve the dispatcher using the provided Dispatcher Id
                var dispatcher = await _dispatcherRepository.GetByIdAsync(createSubscriptionRequest.DispatchId)
                                ?? throw new ArgumentException("Invalid Dispatcher ID.");

                // Retrieve the carrier using the provided Carrier Id
                var carrier = await _carrierRepository.GetByIdAsync(createSubscriptionRequest.CarrierId)
                                ?? throw new ArgumentException("Invalid Carrier ID.");

                // Validate Modules
                var invalidModuleIds = createSubscriptionRequest.RequestedModules
                    .Where(moduleId => _moduleRepository.GetByIdAsync(moduleId) == null);

                if (invalidModuleIds.Any())
                    throw new ArgumentException($"Invalid Module IDs: {string.Join(", ", invalidModuleIds)}");

                // Create a new SubscriptionRequest object and set its properties based on the request data
                var subscriptionRequest = new SubscriptionRequest
                {
                    dispatch_service_id = createSubscriptionRequest.DispatchId,
                    carrier_id = createSubscriptionRequest.CarrierId,
                    status = "Pending",
                    request_date = DateTime.UtcNow
                };

                // Add the new subscription request to the repository asynchronously
                await _subscriptionRepository.AddAsync(subscriptionRequest);
                _logger.LogInformation("Subscription request created with ID: {RequestId}", subscriptionRequest.subscription_request_id);

                // Add each requested module to the RequestModule repository, linking them to the new subscription request
                foreach (var moduleId in createSubscriptionRequest.RequestedModules)
                {
                    var requestModule = new RequestModule
                    {
                        subscription_request_id = subscriptionRequest.subscription_request_id,
                        module_id = moduleId
                    };

                    await _requestModuleRepository.AddAsync(requestModule);
                    _logger.LogInformation("Added Module ID {ModuleId} to Subscription Request ID {RequestId}", moduleId, subscriptionRequest.subscription_request_id);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while creating subscription request");
                throw new ArgumentException("Invalid Dispatcher ID, Carrier ID, or Module ID.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating subscription request");
                // Throw an exception if an unexpected error occurs
                throw new ApplicationException("An unexpected error occurred while creating the subscription request.");
            }
        }

        /// <summary>
        /// Responds to a subscription request by updating its status based on the provided response.
        /// Validates that the request belongs to the specified carrier and ensures the status is either 'Approved' or 'NotApproved'.
        /// If the status is 'Approved', it sets the approval date to the current UTC time.
        /// </summary>
        /// <param name="carrierId">The ID of the carrier responding to the subscription request.</param>
        /// <param name="request">The subscription response containing the request ID and the new status.</param>
        /// <exception cref="ArgumentException">Thrown when the request Id is invalid or the status is not recognized.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the carrier does not own the subscription request.</exception>
        public async Task RespondSubscriptionRequestAsync(int carrierId, SubscriptionResponse request)
        {
            try
            {
                _logger.LogInformation("Carrier {CarrierId} is responding to Subscription Request ID: {RequestId} with status: {Status}", carrierId, request.RequestId, request.Status);

                // Retrieve the subscription request by its Id
                var subscriptionRequest = await _subscriptionRepository.GetByIdAsync(request.RequestId)
                                        ?? throw new ArgumentException("Invalid Request ID.");

                // Validate that the carrier owns the subscription request
                if (subscriptionRequest.carrier_id != carrierId)
                    throw new UnauthorizedAccessException("Carrier does not own this subscription request.");

                // Validate that the status is either 'Approved' or 'NotApproved'
                if (request.Status != "Approved" && request.Status != "NotApproved")
                    throw new ArgumentException("Status must be either 'Approved' or 'NotApproved'.");

                // Update the status of the subscription request
                subscriptionRequest.status = request.Status;

                // If approved, set the approval date to the current UTC time
                if (request.Status == "Approved")
                    subscriptionRequest.approve_date = DateTime.UtcNow;

                // Update the subscription request in the repository
                await _subscriptionRepository.UpdateAsync(subscriptionRequest);
                _logger.LogInformation("Subscription Request ID: {RequestId} updated with status: {Status}", request.RequestId, request.Status);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by Carrier ID: {CarrierId} for Subscription Request ID: {RequestId}", carrierId, request.RequestId);
                throw new UnauthorizedAccessException("Carrier does not own this subscription request.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while responding to subscription request");
                // Throw an exception if any validation errors are found
                throw new ArgumentException("Invalid Request ID or Status.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while responding to subscription request");
                // Throw an exception if an unexpected error occurs
                throw new ApplicationException("An unexpected error occurred while responding to the subscription request.");
            }
        }

        /// <summary>
        /// Retrieves all subscription requests associated with a specific dispatch service Id.
        /// </summary>
        /// <param name="dispatchServiceId">The ID of the dispatch service for which the subscription requests are to be retrieved.</param>
        /// <returns> A collection of <see cref="SubscriptionDto"/> objects containing subscription request details including the request ID, status, and carrier name./// </returns>
        /// <exception cref="ArgumentException">Thrown when the provided dispatch service ID is invalid (less than or equal to zero).</exception>
        public async Task<ServiceResponse<IEnumerable<SubscriptionDto>>> GetAllRequestsByDispatchServiceIdAsync(int dispatchServiceId)
        {
            try
            {
                _logger.LogInformation("Fetching all subscription requests for Dispatch Service ID: {DispatchServiceId}", dispatchServiceId);

                if (dispatchServiceId <= 0)
                    throw new ArgumentException("Invalid Dispatch Service Id");

                // Fetch requests from the repository
                var requests = await _subscriptionRepository.GetRequestsByDispatchServiceIdAsync(dispatchServiceId);
                if (!requests.Any() || requests == null)
                {
                    _logger.LogWarning("No subscription requests found for Dispatch Service ID: {DispatchServiceId}", dispatchServiceId);
                    return ServiceResponse<IEnumerable<SubscriptionDto>>.ErrorResponse("No requests found matching the provided Dispatch Service Id.");
                }

                _logger.LogInformation("Successfully retrieved {RequestCount} subscription requests for Dispatch Service ID: {DispatchServiceId}", requests.Count(), dispatchServiceId);
                return ServiceResponse<IEnumerable<SubscriptionDto>>.SuccessResponse(requests, "Subscription Requests Retrieved Successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid Dispatch Service ID: {DispatchServiceId}", dispatchServiceId);
                return ServiceResponse<IEnumerable<SubscriptionDto>>.ErrorResponse("Invalid Dispatch Service Id");
            }
            catch (KeyNotFoundException kNFex)
            {
                _logger.LogWarning(kNFex, "No subscription requests found for Dispatch Service ID: {DispatchServiceId}", dispatchServiceId);
                return ServiceResponse<IEnumerable<SubscriptionDto>>.ErrorResponse("No requests found matching the provided Dispatch Service Id.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching subscription requests");
                throw new ApplicationException("An unexpected error occurred while fetching subscription requests.");
            }
        }
    }
}