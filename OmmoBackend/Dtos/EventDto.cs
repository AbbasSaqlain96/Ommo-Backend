namespace OmmoBackend.Dtos
{
    public class EventDto
    {
        public int EventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public int TruckId { get; set; }
        public int? TrailerId { get; set; }
        public string Location { get; set; }
        public string Authority { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
        public int? LoadId { get; set; }
        public decimal EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public string CompanyFeeApplied { get; set; }
        public decimal CompanyFeeAmount { get; set; }
        public DateTime? CompanyFeeStatementDate { get; set; }
    }
}
