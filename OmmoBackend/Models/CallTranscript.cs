using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace OmmoBackend.Models
{
    public class CallTranscript
    {
        [Key]
        public Guid transcript_id { get; set; }
        public Guid call_id { get; set; }
        public string speaker { get; set; }
        public string text { get; set; }
        public int message_index { get; set; }
        public DateTime timestamp { get; set; }
    }
}
