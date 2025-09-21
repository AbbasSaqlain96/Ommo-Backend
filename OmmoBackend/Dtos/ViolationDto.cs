namespace OmmoBackend.Dtos
{
    public class ViolationDto
    {
        public int ViolationId { get; set; }
        public string ViolationType { get; set; }
        public string Description { get; set; }
        public int PenaltyPoints { get; set; }
        public decimal FineAmount { get; set; }
    }
}
