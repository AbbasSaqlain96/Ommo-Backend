using OmmoBackend.Helpers.Constants;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Violation
    {
        [Key]
        public int violation_id { get; set; }

        [Required]
        public string violation_type { get; set; }

        [Required]
        public string description { get; set; }

        [Required]
        public int unit { get; set; }

        [Required]
        public string oos { get; set; }
    }
}
