using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class IntegrationEmailProcess
    {
        [Key]
        public int id { get; set; }
        public string message_id { get; set; } = null!;
        public DateTime processed_at { get; set; } = DateTime.UtcNow;
    }
}
