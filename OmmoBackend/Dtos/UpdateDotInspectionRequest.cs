using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class UpdateDotInspectionRequest
    {
        // Performance Event
        [Required]
        public int EventId { get; set; }
        public int? TruckId { get; set; }
        public int? DriverId { get; set; }
        public int? TrailerId { get; set; }
        public string? Location { get; set; }
        public string? Authority { get; set; }
        public DateTime? EventDate { get; set; }
        public string? Description { get; set; }
        public int? LoadId { get; set; }
        public int? EventFee { get; set; }
        public string? FeesPaidBy { get; set; }
        public string? CompanyFeeApplied { get; set; }
        public int? CompanyFeeAmount { get; set; }
        public DateTime? CompanyFeeStatementDate { get; set; }

        // Doc Inspection
        public string? Status { get; set; }
        public int? InspectionLevel { get; set; }
        public string? Citation { get; set; }

        // Doc Inspection Documents
        public IFormFile? DocInspectionDoc { get; set; }
        public string? DocNumber { get; set; }

        // Doc Inspection Violation 
        public List<int?>? Violations { get; set; }
    }
}
