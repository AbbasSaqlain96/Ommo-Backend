using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace OmmoBackend.Models
{
    public class CallTranscript
    {
        [Key]
        public int transcript_id { get; set; }
        public int call_id { get; set; }
        public string speaker { get; set; }
        public string text { get; set; }
        public DateTime timestamp { get; set; }
    }
}
