using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record UnitInfoResult
    {
        public int UnitId { get; set; }
        public string DriverName { get; set; }
        public int TruckId { get; set; }
        public int TrailerId { get; set; }
        public int Speed { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string TruckStatus { get; set; }   
    }
}