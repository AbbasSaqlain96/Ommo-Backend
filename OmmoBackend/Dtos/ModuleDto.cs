namespace OmmoBackend.Dtos
{
    public class ModuleDto
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public List<CompDto> Components { get; set; } = new List<CompDto>();
    }

    public class CompDto
    {
        public int ComponentId { get; set; }
        public string ComponentName { get; set; }
    }
}
