using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record TruckLocationDto
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string LocationState { get; init; }
        public string LocationCity { get; init; }
        public DateTime? LastUpdatedLocation { get; init; }
    }
}