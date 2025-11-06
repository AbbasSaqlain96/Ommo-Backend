namespace OmmoBackend.Dtos
{
    public class RegisterAIAgentResult
    {
        public bool Status { get; set; }
        public Guid AgentId { get; set; }
        public string TwilloNumber { get; set; }
    }
}
