using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public record TrailerDto
    {
        public int TrailerId { get; init; }
        public TrailerType TrailerType { get; init; }
        public int VehicleId { get; init; }
    }
}