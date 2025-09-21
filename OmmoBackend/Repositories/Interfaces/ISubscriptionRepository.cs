using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ISubscriptionRepository : IGenericRepository<SubscriptionRequest>
    {
        Task<IEnumerable<SubscriptionDto>> GetRequestsByDispatchServiceIdAsync(int dispatchServiceId);
    }
}