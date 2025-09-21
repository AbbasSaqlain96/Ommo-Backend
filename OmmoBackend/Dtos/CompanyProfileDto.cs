namespace OmmoBackend.Dtos
{
    public record CompanyProfileDto
    {
        public int CompanyId { get; init; }
        public string Name { get; init; }
        public string Address { get; init; }
        public string Email { get; init; }
        public string Phone { get; init; }
        public int UserCount { get; init; }
        public string CompanyType { get; init; }
        public string CategoryType { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? TaxID { get; init; }
        public string? DOTNumber { get; init; }
        public string? Logo { get; init; }
        public string? MCNumber { get; init; }
    }
}
