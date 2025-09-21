using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class LoadFiltersDto
    {
        // Origin & Destination
        public string Origin { get; init; }
        public string Destination { get; init; }

        // Deadhead Miles (DH-O / DH-D)
        public int MaxOriginDeadheadMiles { get; init; } = 450;
        public int MaxDestinationDeadheadMiles { get; init; } = 450;

        // Availability Dates
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        // Age of posting (Minutes)
        public int MaxAgeMinutes { get; init; } = 4320;

        // Rate Per Mile
        public decimal? RPM { get; init; }

        // Equipment
        public string EquipmentType { get; init; } // CSV e.g. "V,F,R"

        // Capacity
        public int? MaximumLengthFeet { get; init; }
        public int? MaximumWeightPounds { get; init; }

        public string LoadType { get; init; }      // e.g. "FULL", "PARTIAL", "BOTH"
    }
}
