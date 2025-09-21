namespace OmmoBackend.Dtos
{
    public class AssetResponseDto
    {
        public int VehicleId { get; set; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
