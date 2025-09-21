namespace OmmoBackend.Dtos
{
    public record DriverDetailDto
    {
        public string DriverName { get; init; }
        public string DriverLastName { get; init; }
        public string EmploymentType { get; init; }
        public string CDLLicenseNumber { get; init; }
        public string Address { get; init; }
        public string Status { get; init; }
        public string HiringStatus { get; init; }
        public string LicenseState { get; init; }
        public string Email { get; init; }
        public string PhoneNumber { get; init; }
        public int Rating { get; init; }
        public int CompanyId { get; init; }
    }
}
