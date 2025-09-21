using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class TicketFile
    {
        [Key]
        public int issue_file_id { get; set; }
        public string path { get; set; }
        public DateTime created_at { get; set; }
        public int ticket_id { get; set; }
    }
}
