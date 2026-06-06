using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class StripeProcessedEvent
    {
        [Key]
        public string event_id { get; set; }

        public DateTimeOffset processed_at { get; set; }
    }
}
