namespace OmmoBackend.Dtos
{
    public class ClaimInputDto
    {
        public int claim_amount { get; set; }
        public string claim_description { get; set; }
        public string claim_type { get; set; }
        public string status { get; set; }
    }
}
