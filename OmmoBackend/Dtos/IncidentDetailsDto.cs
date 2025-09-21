using OmmoBackend.Models;

namespace OmmoBackend.Dtos
{
    public class IncidentDetailsDto
    {
        public int TruckId { get; set; }
        public string DriverName { get; set; }
        public int TrailerId { get; set; }
        public string EventType { get; set; }
        public string Location { get; set; }
        public string Authority { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
        public int LoadId { get; set; }
        public int EventFee { get; set; }
        public string FeesPaidBy { get; set; }
        public bool CompanyFeeApplied { get; set; }
        public int CompanyFeeAmount { get; set; }
        public DateTime CompanyFeeStatementDate { get; set; }

        public DateTime IncidentDate { get; set; }
        public string IncidentType { get; set; }
        public InvoiceDetailsDto Invoice { get; set; }

        public List<string> IncidentTypes { get; set; }
        public List<string> EquipmentDamages { get; set; }
        public List<string> Images { get; set; }
        public List<IncidentDocDto> Docs { get; set; }
        public List<Claims> Claims { get; set; }
    }
}
