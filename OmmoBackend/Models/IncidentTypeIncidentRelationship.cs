using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class IncidentTypeIncidentRelationship
    {
        [Key]
        public int incidentype_incid_rel_id { get; set; }

        [Required]
        public int incid_type_id { get; set; }

        [Required]
        public int incident_id { get; set; }
    }
}
