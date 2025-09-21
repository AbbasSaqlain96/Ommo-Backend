using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Driver
    {
        [Key]
        public int driver_id { get; set; }
        public string driver_name { get; set; }
        public string last_name { get; set; }

        [Required]
        public EmploymentType employment_type { get; set; }

        [Required]
        public string cdl_license_number { get; set; }

        public string address { get; set; }
        public DriverStatus status { get; set; }

        public HiringStatus hiring_status { get; set; }

        public LicenseState license_state { get; set; }

        [EmailAddress]
        public string email { get; set; }
        [Phone]
        public string phone_number { get; set; }
        [Range(1, 5)]
        public int rating { get; set; }

        public bool is_assign { get; set; }
        public int company_id { get; set; }
    }
}