namespace OmmoBackend.Dtos
{
    public class AgentSettingsResponse
    {
        public Guid AgentGuid { get; set; }
        public string AgentName { get; set; }
        public string WhoWeAre { get; set; }
        public string VoiceGender { get; set; }
        public decimal? FloorRpm { get; set; }
        public decimal? TargetRpm { get; set; }
        public decimal? WalkawayRpm { get; set; }
        public bool ConsentMode { get; set; }
    }
}
