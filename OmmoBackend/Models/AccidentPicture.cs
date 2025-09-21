using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class AccidentPicture
    {
        [Key]
        public int picture_id { get; set; }

        public int accident_id { get; set; }

        public string picture_url { get; set; }

        public DateTime uploaded_at { get; set; } = DateTime.UtcNow;
    }
}