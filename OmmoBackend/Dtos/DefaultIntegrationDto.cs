namespace OmmoBackend.Dtos
{
    public class DefaultIntegrationDto
    {
        public int DefaultIntegrationId { get; set; }
        public string IntegrationName { get; set; }
        public string IntegrationCat { get; set; }
        public string IntegrationDescription { get; set; }
        public string LogoPath { get; set; }
        public string? IntegrationStatus { get; set; }
    }
}
