using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class IncidentEquipDamage
    {

        [Key]
        public int incid_equip_id { get; set; }

        [Required]
        public string incid_equip_name { get; set; }
    }

}
