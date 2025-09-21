using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Call
    {
        [Key]
        public int call_id { get; set; }
        public int user_id { get; set; }
        public string broker_number { get; set; }
        public bool is_broker_already_registered { get; set; }
        public string status_of_call { get; set; }
        public DateTime call_timestamp { get; set; }
        public int load_id { get; set; }
        public string caller_id { get; set; }
        public int company_id { get; set; }
        public int match_id { get; set; }
        public int truckstop_id { get; set; }
        public string loadboard_type { get; set; }
        public string broker_company { get; set; }
    }
}
