namespace OmmoBackend.Dtos
{
    public class NormalizedLoadDto
    {
        public string Origin { get; set; }
        public int? DHO { get; set; }
        public int? DHD { get; set; }
        public string Destination { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Age { get; set; }
        public double? RPM { get; set; }
        public string EquipmentType { get; set; }
        public string? Length { get; set; }
        public int? Weight { get; set; }
        public string LoadType { get; set; }
        public string ClientName { get; set; }
        public string ClientMC { get; set; }
        public string ClientLocation { get; set; }
        public string ClientPhone { get; set; }
        public string ClientEmail { get; set; }
        public string ClientCreditScore { get; set; }
        public string ClientDaysOfPay { get; set; }
        public string Source { get; set; }
        public string MatchID { get; set; }
        public string ID { get; set; }
    }
}
