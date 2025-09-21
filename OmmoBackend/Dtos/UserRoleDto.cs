namespace OmmoBackend.Dtos
{
    public record UserRoleDto
    {
        public int UserId { get; set; }
        public string Name { get; init; }
        public string Email { get; init; }
        public string Phone { get; init; }
        public string Status { get; init; }
        public string RoleName { get; init; }
    }
}
