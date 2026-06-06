using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmmoBackend.Models
{
    public class CallConfirmData
    {
        [Key]
        public Guid c_c_d_id { get; set; }

        public Guid call_id { get; set; }
        public string? broker_name { get; set; }
        public DateTime? pickup_time { get; set; }
        public DateTime? delivery_time { get; set; }
        public decimal? trip_mile { get; set; }
        public decimal? rate_per_mile { get; set; }
        public decimal? final_rate { get; set; }
        public string? origin { get; set; }
        public string? destination { get; set; }

        public string? equipment_type { get; set; }     // e.g. Dry Van, Reefer
        public string? load_type { get; set; }          // Full / Partial
        public string? commodity { get; set; }
        public decimal? weight { get; set; }

        public decimal? load_size { get; set; }  // lbs or kg (as per business rule)



        [ForeignKey(nameof(call_id))]
        public Call Call { get; set; }
    }

    public class CallSentiment
    {
        [Key]
        public Guid c_s_id { get; set; }

        public Guid call_id { get; set; }

        public string sentiment { get; set; } = string.Empty;

        [ForeignKey(nameof(call_id))]
        public Call Call { get; set; }
    }

    public class CallSummaryBullet
    {
        [Key]
        public Guid bullet_id { get; set; }

        public Guid call_id { get; set; }

        public DateTime timestamp { get; set; } = DateTime.UtcNow;

        public string text { get; set; } = string.Empty;

        [ForeignKey(nameof(call_id))]
        public Call Call { get; set; }
    }
}
