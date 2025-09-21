using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record DriverDto
    {   
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public string LastName { get; set; }
        public string EmploymentType { get; set; }
        public string Status { get; set; }
        public string CDLLicenseNumber { get; set; }
        public string LicenseState { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string CurrentMode { get; set; }
        public DateTime LastUpdateMode { get; set; }
        public int Rating { get; set; }
        public string HiringStatus { get; set; }
        public string Address { get; set; }
        public int CompanyId { get; set; }
    }
}