using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class UnitTicket
    {
        [Key]
        public int ticket_id { get; set; }

        [Required]
        public string status { get; set; }

        public int event_id { get; set; }
    }
}
