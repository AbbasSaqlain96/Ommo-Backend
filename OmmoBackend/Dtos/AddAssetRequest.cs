using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public class AddAssetRequest
    {
        public string PlateNumber { get; set; }

        public string PlateState { get; set; }

        public string VinNumber { get; set; }

        public string VehicleType { get; set; }

        public int Year { get; set; }

        public string Trademark { get; set; }

        public bool IsHeadrake { get; set; }

        public bool HaveFlatbed { get; set; }

        public bool HaveLoadbar { get; set; }

        public bool HaveVanStraps { get; set; }

        public int Weight { get; set; }

        public int AxleSpacing { get; set; }

        public int NumOfAxles { get; set; }

        public string? Brand { get; set; }

        public string? Model { get; set; }

        public string? Color { get; set; }

        public string? FuelType { get; set; }

        public string? TrailerType { get; set; }
    }

    public class DocumentUploadDto
    {
        public IFormFile? File { get; set; }
        public string? State { get; set; }
        public int DocTypeId { get; set; }
    }

    public class AddAssetRequestDto 
    {
        public string PlateNumber { get; set; }

        public string PlateState { get; set; }

        public string VinNumber { get; set; }

        public string VehicleType { get; set; }

        public int Year { get; set; }

        public string Trademark { get; set; }

        public bool IsHeadrake { get; set; }

        public bool HaveFlatbed { get; set; }

        public bool HaveLoadbar { get; set; }

        public bool HaveVanStraps { get; set; }

        public int Weight { get; set; }

        public int AxleSpacing { get; set; }

        public int NumOfAxles { get; set; }

        public string? Brand { get; set; }

        public string? Model { get; set; }

        public string? Color { get; set; }

        public string? FuelType { get; set; }

        public string? TrailerType { get; set; }

        public List<DocumentUploadDto?> Documents { get; set; }
    }
}