using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public class UserCompanyDto
    {
        // User Info
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int RoleId { get; set; }
        public UserStatus Status { get; set; }

        // Company Info
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }
        public int CompanyType { get; set; }
        public CompanyStatus CompanyStatus { get; set; }
        public string CompanyDotNumber { get; set; }
        public string CompanyMCNumber { get; set; }
    }
}
