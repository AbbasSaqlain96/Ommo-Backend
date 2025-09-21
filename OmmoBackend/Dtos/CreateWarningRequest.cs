namespace OmmoBackend.Dtos
{
    public class CreateWarningRequest
    {
        public WarningEventInfoDto WarningEventInfoDto { get; set; }
        public WarningDocumentsDto? WarningDocumentsDto { get; set; }
        public List<int> Violations { get; set; }
    }

    public class WarningEventInfoDto
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

    public class WarningDocumentsDto
    {
        public string? DocNumber { get; set; }
        public IFormFile? DocPath { get; set; }
      }
}
