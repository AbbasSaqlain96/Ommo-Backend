using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Notification
    {
        [Key]
        public int id { get; set; }
        public int company_id { get; set; }
        public string module { get; set; } = string.Empty;
        public string component { get; set; } = string.Empty;
        public int access_level { get; set; }
        public string message { get; set; } = string.Empty;
        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}
