namespace OmmoBackend.Dtos
{
    public record DriverListDto
    {
        public int DriverId { get; init; }
        public string DriverName { get; init; }
        public string Status { get; init; }
        public int Rating { get; init; }
    }
}
