namespace OmmoBackend.Dtos
{
    public class CustomPackageRequestDto
    {
        public string Email { get; set; } = default!;
        public int EstMinutes { get; set; }
        public int Concurrency { get; set; }
        public string? Message { get; set; }
        public int AllowedUsers { get; set; }
    }
}
