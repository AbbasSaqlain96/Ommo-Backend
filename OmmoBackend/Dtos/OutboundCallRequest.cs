namespace OmmoBackend.Dtos
{
    public class OutboundCallRequest
    {
        public int Mileage { get; set; }
        public decimal RateTotal { get; set; }
        public decimal LoadRpm { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Client info
        public string ClientPhone { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientCompany { get; set; } = string.Empty;

        // Loadboard / matching
        public Guid? MatchId { get; set; }         // nullable because sometimes may not exist
        public string? TruckstopId { get; set; }   // keep as string if ID may contain non-numeric chars
        public string LoadboardType { get; set; } = string.Empty; // "DAT" or "Truckstop"
    }

    public record LoadInfo(
    int Mileage,
    decimal RateTotal,
    decimal LoadRpm,
    string Origin,
    string Destination,
    DateTime FromDate,
    DateTime ToDate
    );

    public record ClientInfo(
        string ClientPhone,
        string ClientEmail,
        string ClientCompany
    );

    public record OutboundCallResult(
        string UltravoxCallId,
        string TwilioCallSid,
        string Status
    );
}
