namespace OmmoBackend.Dtos
{
    public class AgentAIConfig
    {
        public Guid AgentProfileId { get; set; }
        public Guid PersonaId { get; set; }
        public Guid PromptId { get; set; }
        public string Description { get; set; }
    }
}
