namespace OmmoBackend.Dtos
{
    public class IntegrationDto
    {
        public int IntegrationId { get; set; }
        public int DefaultIntegrationId { get; set; }
        public string IntegrationStatus { get; set; }
        public DateTime LastUpdated { get; set; }

        // From DefaultIntegrations
        public string IntegrationName { get; set; }
        public string IntegrationDescription { get; set; }
        public string LogoPath { get; set; }
    }
}
