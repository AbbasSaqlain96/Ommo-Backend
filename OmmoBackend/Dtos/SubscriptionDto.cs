using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record SubscriptionDto
    {
        public int RequestId { get; init; }
        public string Status { get; init; }
        public string CarrierName { get; init; }
    }
}