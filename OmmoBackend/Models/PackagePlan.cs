using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmmoBackend.Models
{
    public class PackagePlan
    {
        [Key]
        public int plan_id { get; set; }

        [Required]
        public PlanType plan_type { get; set; }

        [Required]
        [MaxLength(255)]
        public string plan_name { get; set; }

        [Required]
        public int concurrency { get; set; }

        [Required]
        public PlanInterval interval { get; set; }

        [Required]
        public string stripe_price_id { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal price { get; set; }

        [Required]
        public int allowed_users { get; set; }

        // Nullable because only custom plans have company_id
        public int? company_id { get; set; }

        [Required]
        public int est_minute { get; set; }

        [Required]
        public PlanStatus plan_status { get; set; } = PlanStatus.active;

        [Required]
        public DateTime created_at { get; set; }
        
        [Required]
        public bool is_transcript_allowed { get; set; } = false;

        [Required]
        public bool is_takeover_allowed { get; set; } = false;
    }
}
