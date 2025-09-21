using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record TrailerLocationDto
    {
        public string VehicleType { get; init; }
        public int VehicleId { get; init; }
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public string LocationState { get; init; }
        public string LocationCity { get; init; }
        public DateTime? LastUpdatedLocation { get; init; }
    }
}