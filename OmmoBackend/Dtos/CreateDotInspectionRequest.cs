namespace OmmoBackend.Dtos
{
    public class CreateDotInspectionRequest
    {
        public DotInspectionEventInfoDto dotInspectionEventInfoDto { get; set; }
        public DotInspectionDocInspectionInfoDto docInspectionDto { get; set; }
        public DocInspectionDocuments docInspectionDocumentsDto { get; set; }
        public List<int> Violations { get; set; }
    }

    public class DotInspectionEventInfoDto
    {
        public int TruckId { get; set; }
        public int DriverId { get; set; }
        public int? TrailerId { get; set; }
        public string Location { get; set; }
        public string Authority { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
        public int? LoadId { get; set; }
        public int EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public string CompanyFeeApplied { get; set; }
        public int CompanyFeeAmount { get; set; }
        public DateTime CompanyFeeStatementDate { get; set; }
    }

    public class DotInspectionDocInspectionInfoDto 
    {
        public string Status { get; set; }
        public int InspectionLevel { get; set; }
        public string Citation { get; set; }
    }

    public class DocInspectionDocuments 
    {
        public string DocNumber { get; set; }
        public IFormFile DocInspectionDoc { get; set; }
    }
}
