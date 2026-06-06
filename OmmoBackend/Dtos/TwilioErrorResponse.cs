namespace OmmoBackend.Dtos
{
    public class TwilioErrorResponse
    {
        public int code { get; set; }
        public string message { get; set; }
        public string more_info { get; set; }
        public int status { get; set; }
    }
}
