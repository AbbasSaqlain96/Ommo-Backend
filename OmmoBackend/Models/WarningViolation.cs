using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class WarningViolation
    {
        [Key]
        public int warning_violation_id { get; set; }

        [Required]
        public int warning_id { get; set; }

        [Required]
        public int violation_id { get; set; }

        [Required]
        public DateTime violation_date { get; set; }
    }
}
