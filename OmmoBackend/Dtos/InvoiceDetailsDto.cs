namespace OmmoBackend.Dtos
{
    public class InvoiceDetailsDto
    {
        public int Amount { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? ClosureDate { get; set; }
    }
}