using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class UpdateIncidentRequest
    {
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
        public List<int?>? IncidentTypeIds { get; set; }
        public List<int?>? EquipmentDamageIds { get; set; }
        public List<IFormFile?>? Images { get; set; }
        public string? DocNumber { get; set; }
        public IFormFile? DocFile { get; set; }
 
        [FromForm(Name = "ClaimInfoJson")]
        public string? ClaimInfoJson { get; set; }
    }

    public class UpdateClaimRequest
    {
        public int? ClaimId { get; set; }
        public string ClaimDescription { get; set; }
        public int EventId { get; set; }
        public string ClaimType { get; set; }
        public int ClaimAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateDocumentRequest
    {
        public int? DocTypeId { get; set; } = 28; // default to "incident_doc"
        public string DocNumber { get; set; }
        public IFormFile File { get; set; }
    }
}
