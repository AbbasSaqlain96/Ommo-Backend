using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class UpdateAccidentRequest
    {
        public UpdateAccidentEventInfoDto? updateAccidentEventInfoDto { get; set; }
        public UpdateAccidentInfoDto? updateAccidentInfoDto { get; set; }
        public UpdateAccidentDocumentDto? updateAccidentDocumentDto { get; set; }
        public UpdateAccidentImageDto? updateAccidentImageDto { get; set; }

        [FromForm(Name = "ClaimInfoJson")]
        public string? ClaimInfoJson { get; set; }
    }

    public class UpdateAccidentEventInfoDto
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
    }

    public class UpdateAccidentInfoDto
    {
        public bool? DriverFault { get; set; }
        public bool? AlcoholTest { get; set; }
        public DateTime? DrugTestDateTime { get; set; }
        public DateTime? AlcoholTestDateTime { get; set; }
        public bool? HasCasualties { get; set; }
        public bool? DriverDrugTested { get; set; }
        public int? TicketId { get; set; }
    }

    public class UpdateAccidentDocumentDto
    {
        public IFormFile? PoliceReportFile { get; set; }
        public string? PoliceReportNumber { get; set; }
        public IFormFile? DriverReportFile { get; set; }
        public string? DriverReportNumber { get; set; }
    }

    public class UpdateAccidentImageDto
    {
        public List<IFormFile?>? AccidentImages { get; set; }
    }

    public class UpdateAccidentClaimDto 
    {
        public int? ClaimId { get; set; }
        public int? ClaimAmount { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimDescription { get; set; }
        public string? Status { get; set; }
    }
}
