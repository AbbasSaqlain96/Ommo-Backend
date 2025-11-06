namespace OmmoBackend.Dtos
{
    public class ToggleIntegrationStatusRequest
    {
        public int IntegrationId { get; set; }
        public string RequestedByEmail { get; set; }
    }
}
