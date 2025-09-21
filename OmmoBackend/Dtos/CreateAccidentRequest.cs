using Microsoft.AspNetCore.Mvc;

namespace OmmoBackend.Dtos
{
    public class CreateAccidentRequest
    {
        public AccidentEventInfoDto EventInfo { get; set; }
        public AccidentInfoDto AccidentInfo { get; set; }
        public AccidentDocumentDto AccidentDocumentDto { get; set; }
        public AccidentImageDto AccidentImageDto { get; set; }

        [FromForm(Name = "ClaimInfoJson")]
        public string? ClaimInfoJson { get; set; }
    }

    public class AccidentEventInfoDto
    {
        public DateTime EventDate { get; set; }
        public int DriverId { get; set; }
        public int TruckId { get; set; }
        public int? TrailerId { get; set; }
        public int? LoadId { get; set; }
        public string Location { get; set; }
        public string Authority { get; set; }
        public string Description { get; set; }
        public int EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public string CompanyFeeApplied { get; set; }
        public int CompanyFeeAmount { get; set; }
        public DateTime? CompanyFeeStatementDate { get; set; }
    }

    public class AccidentInfoDto
    {
        public bool DriverFault { get; set; }
        public bool AlcoholTest { get; set; }
        public DateTime? DrugTestDateTime { get; set; }
        public DateTime? AlcoholTestDateTime { get; set; }
        public bool HasCasualty { get; set; }
        public bool DriverDrugTest { get; set; }
        public int? TicketId { get; set; }
    }

    public class AccidentDocumentDto
    {
        public IFormFile? PoliceReportFile { get; set; }
        public string? PoliceReportNumber { get; set; }
        public IFormFile? DriverReportFile { get; set; }
        public string? DriverReportNumber { get; set; }
    }

    public class AccidentImageDto
    {
        public List<IFormFile>? AccidentImages { get; set; }
    }

}
