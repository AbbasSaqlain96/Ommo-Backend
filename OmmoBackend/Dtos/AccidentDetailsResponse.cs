namespace OmmoBackend.Dtos
{
    public record AccidentDetailsResponse
    {
        public AccidentDetailDto accidentDetailDto { get; set; }
        public List<ClaimDto> accidentClaimDtos { get; set; }
    }

    public class AccidentDetailDto
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
        public DateTime? CompanyFeeStatementDate { get; set; }
        public bool DriverFault { get; set; }
        public bool AlcoholTest { get; set; }
        public DateTime? DrugTestDateTime { get; set; }
        public DateTime? AlcoholTestDateTime { get; set; }
        public bool HasCasualties { get; set; }
        public bool DriverDrugTested { get; set; }
        public int? TicketId { get; set; }
        public string PoliceDocNumber { get; set; }
        public string PoliceReport { get; set; }
        public string DriverDocNumber { get; set; }
        public string DriverReport { get; set; }
        public List<string> AccidentPictures { get; set; }
    }

    public class ClaimDto
    {
        public int ClaimId { get; set; }
        public string ClaimType { get; set; }
        public string ClaimStatus { get; set; }
        public int ClaimAmount { get; set; }
        public DateTime ClaimCreatedAt { get; set; }
        public string ClaimDescription { get; set; }
    }
}