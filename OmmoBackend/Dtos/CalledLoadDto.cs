namespace OmmoBackend.Dtos
{
    public class CalledLoadDto
    {
        public string Source { get; set; }
        public string MatchId { get; set; }
        public string TruckStopId { get; set; }
        public DateTime CalledAtUtc { get; set; }
    }
}
