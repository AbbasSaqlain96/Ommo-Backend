using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class SubscriptionRequest
    {
        [Key]
        public int subscription_request_id { get; set; }
        public int carrier_id { get; set; }
        public int dispatch_service_id { get; set; }
        public string status { get; set; }
        public DateTime request_date { get; set; }
        public DateTime approve_date { get; set; }
    }
}