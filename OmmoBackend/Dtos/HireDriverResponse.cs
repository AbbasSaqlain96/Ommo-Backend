namespace OmmoBackend.Dtos
{
    public class HireDriverResponse
    {
        public int DriverId { get; set; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
