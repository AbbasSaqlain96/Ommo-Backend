namespace OmmoBackend.Dtos
{
    public class HireDriverRequest
    {
        public string DriverName { get; set; }
        public string DriverLastName { get; set; }
        public string EmploymentType { get; set; }
        public string CDLLicenseNumber { get; set; }
        public string Address { get; set; }
        public string LicenseState { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int Rating { get; set; }
    }

    public class HireDriverRequestDto
    {
        public string DriverName { get; set; }
        public string DriverLastName { get; set; }
        public string EmploymentType { get; set; }
        public string CDLLicenseNumber { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public string HiringStatus { get; set; }
        public string LicenseState { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int Rating { get; set; }
        public List<DocumentDto> Documents { get; set; }
    }
}