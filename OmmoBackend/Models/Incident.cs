using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Incident
    {
        [Key]
        public int incident_id { get; set; }

        public int event_id { get; set; }
    }
}
