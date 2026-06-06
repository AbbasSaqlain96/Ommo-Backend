namespace OmmoBackend.Dtos
{
    public class TwilioPhoneNumber
    {
        public string phone_number { get; set; }
        public TwilioCapabilities capabilities { get; set; }
        public string address_requirements { get; set; }
        public bool beta { get; set; }
    }
}
