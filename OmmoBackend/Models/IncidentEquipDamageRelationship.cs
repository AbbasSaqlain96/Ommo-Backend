using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class IncidentEquipDamageRelationship
    {
        [Key]
        public int incid_equip_relation_id { get; set; }

        [Required]
        public int incid_equip_id { get; set; }

        [Required]
        public int incident_id { get; set; }
    }
}
