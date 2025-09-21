using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record TruckTrackingDto
    {
        public decimal? Odometer { get; init; }
        public DateTime? LastUpdateOdometer { get; init; }
        public decimal? Speed { get; init; }
        public DateTime? LastUpdateSpeed { get; init; }
        public decimal? Mileage { get; init; }
        public DateTime? LastUpdatedMileage { get; init; }
    }
}