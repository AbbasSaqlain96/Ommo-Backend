
namespace OmmoBackend.Dtos
{
    public class DotInspectionDetailsDto
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
        public int EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public bool CompanyFeeApplied { get; set; }
        public int CompanyFeeAmount { get; set; }
        public DateTime CompanyFeeStatementDate { get; set; }
        public string Status { get; set; }
        public int InspectionLevel { get; set; }
        public string Citation { get; set; }
        public List<DocInspectionDocumentsDto> Docs { get; set; }
        public List<int> ViolationIds { get; set; }
    }

    public class DocInspectionDocumentsDto 
    {
        public string DocNumber { get; set; }
        public string DocPath { get; set; }
    }
}
