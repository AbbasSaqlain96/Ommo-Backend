namespace OmmoBackend.Models
{
    public class CallTakeoverInfo
    {
        public Guid CallId { get; set; }

        public int CompanyId { get; set; }

        public string? TwilioCallSid { get; set; }
    }
}
