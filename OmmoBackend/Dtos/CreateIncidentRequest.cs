using Microsoft.AspNetCore.Mvc;

namespace OmmoBackend.Dtos
{
    public class CreateIncidentRequest
    {
        public IncidentEventInfoDto EventInfo { get; set; }
        public IncidentInfoDto IncidentInfo { get; set; }

        public List<IFormFile> Images { get; set; }

        // Only one document file is allowed
        public string DocNumber { get; set; }
        public IFormFile DocFile { get; set; }

        [FromForm(Name = "ClaimInfoJson")]
        public string? ClaimInfoJson { get; set; }
    }

    public class IncidentEventInfoDto
    {
        public int TruckId { get; set; }
        public int DriverId { get; set; }
        public int? TrailerId { get; set; }
        public string Location { get; set; }
        public string Authority { get; set; }
        public DateTime EventDate { get; set; }
        public int? LoadId { get; set; }
        public int? EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public string CompanyFeeApplied { get; set; }
        public int? CompanyFeeAmount { get; set; }
        public DateTime? CompanyFeeStatementDate { get; set; }
    }

    public class IncidentInfoDto
    {
        public string Description { get; set; }
        public List<int> IncidentTypeIds { get; set; } = new();
        public List<int> EquipmentDamageIds { get; set; } = new();
    }

    public class IncidentDocumentDto
    {
        public int DocTypeId { get; set; } = 28; // default to "incident_doc"
        public string DocNumber { get; set; }
        public IFormFile File { get; set; }
    }

    public class IncidentClaimsDto
    {
        public int ClaimAmount { get; set; }
        public string ClaimDescription { get; set; }
        public string ClaimType { get; set; }
        public string Status { get; set; }
    }
}
