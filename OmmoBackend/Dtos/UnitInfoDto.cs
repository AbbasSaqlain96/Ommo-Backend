using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record UnitInfoDto
    {
        public int UnitId { get; set; }
        public string DriverName { get; init; }
        public int TruckId { get; init; }
        public int TrailerId { get; init; }
        public string UnitStatus { get; init; }
        public int Speed { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string TruckStatus { get; set; }
        public int CompanyId { get; set; }
    }
}