using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class SupportRequest
    {
        [Key]
        public int support_request_id { get; set; }

        [Required]
        public string subject { get; set; }

        [Required]
        public string message { get; set; }

        [Required]
        public string contact_email { get; set; }

        [Required]
        public string status { get; set; } = "pending";

        [Required]
        public bool is_ommo_customer { get; set; } = false;

        [Required]
        public DateTimeOffset created_at { get; set; } = DateTimeOffset.UtcNow;
    }
}
