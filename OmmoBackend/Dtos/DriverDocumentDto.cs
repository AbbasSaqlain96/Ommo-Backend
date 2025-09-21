namespace OmmoBackend.Dtos
{
    public record DriverDocumentDto
    {
        public string DocName { get; init; }
        public string URL { get; init; }
        public string Status { get; init; }
        public string EndDate { get; init; }
    }
}
