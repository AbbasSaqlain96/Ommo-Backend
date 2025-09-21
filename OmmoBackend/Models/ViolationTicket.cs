using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class ViolationTicket
    {
        [Key]
        public int ticket_violation_id { get; set; }

        public int ticket_id { get; set; }

        public int violation_id { get; set; }

        public DateTime violation_date { get; set; }
    }
}
