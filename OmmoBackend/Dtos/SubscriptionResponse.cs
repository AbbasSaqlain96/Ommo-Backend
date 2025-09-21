using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record SubscriptionResponse
    {
        public int RequestId { get; set; }
        
        public string Status { get; set; } // Should be either "Approved" or "NotApproved"
    }
}