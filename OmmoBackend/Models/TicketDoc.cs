using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class TicketDoc
    {
        [Key]
        public int ticket_doc_id { get; set; }
        public int? doc_type_id { get; set; }
        public int? ticket_id { get; set; }
        public string file_path { get; set; }
        public TicketDocStatus status { get; set; }

        [Required]
        public string doc_number { get; set; }
    }
}
