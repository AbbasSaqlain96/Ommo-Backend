namespace OmmoBackend.Dtos
{
    public class TwilioStatusCallbackRequest
    {
        public string CallSid { get; set; }
        public string CallStatus { get; set; }
    }
}
