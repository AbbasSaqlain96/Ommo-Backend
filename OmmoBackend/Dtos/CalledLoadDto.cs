namespace OmmoBackend.Dtos
{
    public class CalledLoadDto
    {
        public string Source { get; set; }           // DAT | truckstop
        public string? ReferenceId { get; set; }     // unified external ID
        public DateTime CalledAtUtc { get; set; }
    }

}
