using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task CreateSubscriptionRequestAsync(CreateSubscriptionRequest createSubscriptionRequest);
        Task RespondSubscriptionRequestAsync(int carrierId, SubscriptionResponse request);
        Task <ServiceResponse<IEnumerable<SubscriptionDto>>> GetAllRequestsByDispatchServiceIdAsync(int dispatchServiceId);
    }
}