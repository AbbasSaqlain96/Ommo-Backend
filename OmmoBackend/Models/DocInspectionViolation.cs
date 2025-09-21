using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class DocInspectionViolation
    {
        [Key]
        public int doc_inspection_violation_id { get; set; }

        [Required]
        public int doc_inspection_id { get; set; }

        [Required]
        public int violation_id { get; set; }

        [Required]
        public DateTime violation_date { get; set; }
    }
}
