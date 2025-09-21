using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class CreateTicketRequest
    {
        public TicketEventInfoDto EventInfo { get; set; }
        public TicketInfoDto TicketInfo { get; set; }
        public TicketInfoDocumentDto TicketDocument { get; set; }
        public List<int> Violations { get; set; }
        public TicketImageDto TicketImageDto { get; set; }
    }

    public class TicketEventInfoDto
    {
        public int TruckId { get; set; }
        public int DriverId { get; set; }
        public int? TrailerId { get; set; }
        public string Location { get; set; }
        public string Authority { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
        public int? LoadId { get; set; }
        public int? EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public string CompanyFeeApplied { get; set; }
        public int? CompanyFeeAmount { get; set; }
        public DateTime? CompanyFeeStatementDate { get; set; }
    }

    public class TicketInfoDto
    {
        public string Status { get; set; } // "new", "closed", "in court"
    }
    
    public class TicketInfoDocumentDto
    {
        public string DocNumber { get; set; }
        public IFormFile Document { get; set; }
    }

    public class TicketImageDto
    {
        public List<IFormFile> TicketImages { get; set; } = new List<IFormFile>();
    }
}