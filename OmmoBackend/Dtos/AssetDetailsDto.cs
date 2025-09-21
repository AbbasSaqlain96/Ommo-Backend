namespace OmmoBackend.Dtos
{
    public class AssetDetailsDto
    {
        public AssetDetailTruckDto Truck { get; set; }
        public string TrailerType { get; set; }
        public List<VehicleAttributeDto> Attributes { get; set; }
        public List<VehicleDocumentDto> Documents { get; set; }
    }

    public class AssetDetailTruckDto 
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public string FuelType { get; set; }
        public string Color { get; set; }
        public string TruckStatus { get; set; }
    }

    public class AssetDetailTrailerDto
    {
        public string Trailer_Type { get; set; }
    }

    public class VehicleAttributeDto
    {
        public int AttributeId { get; set; }
        public int VehicleId { get; set; }
        public bool IsHeadrake { get; set; }
        public bool HaveFlatbed { get; set; }
        public bool HaveLoadbar { get; set; }
        public bool HaveVanStraps { get; set; }
        public int Weight { get; set; }
        public int AxleSpacing { get; set; }
        public int NumOfAxles { get; set; }
        public string VehicleType { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class VehicleDocumentDto
    {
        public string DocTypeName { get; set; }
        public string Path { get; set; }
        public string State { get; set; }
    }
}
