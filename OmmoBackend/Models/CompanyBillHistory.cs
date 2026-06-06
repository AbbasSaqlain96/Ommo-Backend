using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmmoBackend.Models
{
    public class CompanyBillHistory
    {
        [Key]
        public int company_bill_history_id { get; set; }

        [Required]
        public int company_id { get; set; }

        [Required]
        public DateTimeOffset start_date { get; set; }

        [Required]
        public DateTimeOffset end_date { get; set; }

        [Required]
        public int minutes_used { get; set; } = 0;

        [Required]
        public int minute_unused { get; set; } = 0;

        public int? package_id { get; set; }

        [Required]
        public string package_name { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal amount { get; set; } = 0;

        [Required]
        public DateTimeOffset created_at { get; set; }
    }
}
