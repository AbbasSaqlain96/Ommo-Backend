using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record TruckDto
    {
        public int TruckId { get; init; }
        public string Brand { get; init; }
        public int VehicleId { get; set; }
        public string Model { get; init; }
        public string FuelType { get; set; }
        public string Color { get; init; }
    }
}