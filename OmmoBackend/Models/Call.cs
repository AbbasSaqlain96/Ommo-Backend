using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Call
    {
        [Key]
        public Guid call_id { get; set; }
        public int user_id { get; set; }
        public string broker_number { get; set; }
        public bool is_broker_already_registered { get; set; }
        public string status_of_call { get; set; }
        public DateTime call_timestamp { get; set; }
        public int load_id { get; set; }
        public string? caller_id { get; set; }
        public int company_id { get; set; }
        public string reference_id { get; set; }
        public string loadboard_type { get; set; }
        public string broker_company { get; set; }
        public string? twilio_call_sid { get; set; }
        public string call_result { get; set; }
        public DateTime? call_end_time { get; set; }
        public int? call_duration { get; set; }

        public int? last_uvx_index_fetched { get; set; }
        public bool is_transcript_complete { get; set; } = false;   // <-- NEW
        public bool is_ai_processing_complete { get; set; } = false;

        public DateTime? last_ai_processed { get; set; }


    }
}
