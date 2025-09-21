using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public class TruckInfoDto
    {
        public TruckDto Truck { get; set; }
        public TruckTrackingDto TruckTracking { get; set; }
        public TruckLocationDto TruckLocation { get; set; }
    }
}