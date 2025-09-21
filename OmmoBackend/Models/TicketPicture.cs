using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class TicketPicture
    {
        [Key]
        public int picture_id { get; set; }

        [Required]
        public int ticket_id { get; set; }

        [Required]
        public string picture_url { get; set; }

        [Required]
        public DateTime upload_date { get; set; } = DateTime.UtcNow;
    }
}
