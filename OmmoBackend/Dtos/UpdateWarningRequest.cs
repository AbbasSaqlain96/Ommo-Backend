using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class UpdateWarningRequest
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

        // Warning Documents
        public IFormFile? WarningDoc { get; set; }
        public string? DocNumber { get; set; }

        // Warning Violation
        public List<int?>? Violations { get; set; }
    }
}
