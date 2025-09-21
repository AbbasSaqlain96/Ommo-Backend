namespace OmmoBackend.Models
{
    public class SendEmail
    {
        public int id { get; set; }
        public string send_to { get; set; }
        public string subject { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? sent_at { get; set; }
        public string? error_message { get; set; }
    }
}
