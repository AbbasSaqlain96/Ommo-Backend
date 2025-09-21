using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class SubscriptionRepository : GenericRepository<SubscriptionRequest>, ISubscriptionRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SubscriptionRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the SubscriptionRepository class with the specified database context.
        /// </summary>
        public SubscriptionRepository(AppDbContext dbContext, ILogger<SubscriptionRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all subscription requests for a given dispatch service Id.
        /// </summary>
        /// <param name="dispatchServiceId">The Id of the dispatch service for which subscription requests are to be retrieved.</param>
        /// <returns>A collection of <see cref="SubscriptionDto"/> objects containing details of the subscription requests.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided dispatch service Id does not exist in the database.</exception>
        public async Task<IEnumerable<SubscriptionDto>> GetRequestsByDispatchServiceIdAsync(int dispatchServiceId)
        {
            _logger.LogInformation("Fetching subscription requests for DispatchServiceId: {DispatchServiceId}", dispatchServiceId);

            if (dispatchServiceId <= 0)
            {
                _logger.LogWarning("Invalid Dispatch Service Id: {DispatchServiceId}. Must be greater than 0.", dispatchServiceId);
                throw new ArgumentOutOfRangeException(nameof(dispatchServiceId), "Dispatch Service Id must be greater than 0.");
            }
            
            // Validate that the Dispatch service Id exists in the database
            if (!await DispatchServiceExistsAsync(dispatchServiceId))
            {
                _logger.LogWarning("Dispatch Service Id {DispatchServiceId} does not exist.", dispatchServiceId);
                throw new KeyNotFoundException("Dispatch_Service_ID does not exist.");
            }

            try
            {
                // Retrieve subscription requests associated with the dispatch service Id, including the related Carrier details
                var subscriptionRequests = await (from sr in _dbContext.subscription_request
                                                  join carrier in _dbContext.carrier
                                                  on sr.carrier_id equals carrier.carrier_id
                                                  join company in _dbContext.company
                                                  on carrier.company_id equals company.company_id
                                                  where sr.dispatch_service_id == dispatchServiceId
                                                  select new SubscriptionDto
                                                  {
                                                      RequestId = sr.subscription_request_id,
                                                      Status = sr.status,
                                                      CarrierName = company.name // Map CompanyName to CarrierName
                                                  }).ToListAsync();

                _logger.LogInformation("Retrieved {Count} subscription requests for DispatchServiceId: {DispatchServiceId}", subscriptionRequests.Count, dispatchServiceId);
                return subscriptionRequests;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error while retrieving subscription requests for DispatchServiceId: {DispatchServiceId}", dispatchServiceId);
                throw new DataAccessException("An error occurred while retrieving subscription requests from the database.", dbEx);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while processing subscription requests for DispatchServiceId: {DispatchServiceId}", dispatchServiceId);
                // Handle invalid operations, such as null references during joins
                throw new InvalidOperationException("An error occurred while processing the subscription request data.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving subscription requests for DispatchServiceId: {DispatchServiceId}", dispatchServiceId);
                throw new ApplicationException("An unexpected error occurred while retrieving subscription requests.");
            }
        }

        // Validate DispatchService existence
        private async Task<bool> DispatchServiceExistsAsync(int dispatchServiceId)
        {
            _logger.LogDebug("Checking existence of DispatchServiceId: {DispatchServiceId}", dispatchServiceId);
            return await _dbContext.dispatch_service.AnyAsync(ds => ds.dispatch_service_id == dispatchServiceId);
        }
    }
}