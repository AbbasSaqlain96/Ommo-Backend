using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Warning
    {
        [Key]
        public int warning_id { get; set; }

        [Required]
        public int event_id { get; set; }
    }
}
