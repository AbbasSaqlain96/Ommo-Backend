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
        public int wieght { get; set; }
        public int length { get; set; }
        public string Commodity { get; set; }
        public string Equipment_Type { get; set; }


        //Add Equipment_Type. into Payload as String Array

        public string Reference_ID { get; set; }

        // Client info
        public string ClientPhone { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientCompany { get; set; } = string.Empty;

        public string LoadboardType { get; set; } = string.Empty; // "DAT" or "Truckstop"
    }

    public record LoadInfo(
    int Mileage,
    decimal RateTotal,
    decimal LoadRpm,
    string Origin,
    string Destination,
    string Reference_ID,
    DateTime FromDate,
    DateTime ToDate,
    int wieght,
    int length,
    string commodity,
    string equipment_type
   
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
