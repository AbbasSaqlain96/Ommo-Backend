using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class IncidentType
    {
        [Key]
        public int incid_type_id { get; set; }

        [Required]
        public string incid_type_name { get; set; }
    }
}
