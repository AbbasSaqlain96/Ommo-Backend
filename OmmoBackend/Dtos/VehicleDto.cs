using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public string PlateNumber { get; set; }
        public string PlateState { get; set; }
        public string VinNumber { get; set; }
        public string VehicleType { get; set; }
        public int Rating { get; set; }
        public bool IsAssigned { get; set; }
        public int Year { get; set; }
        public string Trademark { get; set; }
        public string Status { get; set; }
    }
}
