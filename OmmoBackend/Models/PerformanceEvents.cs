using System.ComponentModel.DataAnnotations;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class PerformanceEvents
    {
        [Key]
        public int event_id { get; set; }
        public EventType event_type { get; set; }
        public int driver_id { get; set; }
        public int truck_id { get; set; }
        public int trailer_id { get; set; }
        public string location { get; set; }  
        public EventAuthority authority { get; set; }  
        public string description { get; set; }  
        public int? load_id { get; set; }  
        public int event_fees { get; set; }  
        public FeesPaidBy fees_paid_by { get; set; }  
        public bool company_fee_applied { get; set; }  
        public int company_fee_amount { get; set; }  
        public DateTime? company_fee_statement_date { get; set; }
        public DateTime date { get; set; }
        public int company_id { get; set; }
    }
}
