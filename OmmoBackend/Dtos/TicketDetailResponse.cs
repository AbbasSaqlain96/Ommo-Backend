using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public class TicketDetailResponse
    {
        public TicketDetails TicketDetails { get; set; }
    }

    public class TicketDetails
    {
        public int TruckId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public int TrailerId { get; set; }
        public string Location { get; set; }
        public string EventType { get; set; }
        public string Authority { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
        public int LoadId { get; set; }
        public int EventFees { get; set; }
        public string FeesPaidBy { get; set; }
        public bool CompanyFeeApplied { get; set; }
        public int CompanyFeeAmount { get; set; }
        public DateTime? CompanyFeeStatementDate { get; set; }
        public string TicketStatus { get; set; }
        public string DocNumber { get; set; }
        public string DocumentPath { get; set; }
        public string TicketDocumentURL { get; set; }
        public List<int> ViolationIds { get; set; }
        public List<string> TicketImages { get; set; }
    }

    public class ViolationDetail
    {
        public string ViolationType { get; set; }
        public string ViolationDescription { get; set; }
        public int ViolationPenaltyPoint { get; set; }
        public decimal ViolationFineAmount { get; set; }
    }
}
