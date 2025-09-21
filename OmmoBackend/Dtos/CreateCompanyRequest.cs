using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using OmmoBackend.Validators;

namespace OmmoBackend.Dtos
{
    public record CreateCompanyRequest
    {
        //[Required(ErrorMessage = "Company name is required.")]
        //[StringLength(100, ErrorMessage = "Company name cannot be longer than 100 characters.")]
        public string Name { get; init; }

        //[EmailOrPhoneRequired("Phone", ErrorMessage = "Either Email or Phone must be provided.")]
        //[EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; init; }

        //[Required(ErrorMessage = "Address is required.")]
        //[StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters.")]
        public string Address { get; init; }

        //[EmailOrPhoneRequired("Email", ErrorMessage = "Either Email or Phone must be provided.")]
        //[PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; init; }

        //[MCNumberValidation("CompanyType", ErrorMessage = "MC number is required for company type 1.")]
        public string? MCNumber { get; init; }

        //[Required(ErrorMessage = "Company type is required.")]
        //[CompanyTypeValidation(ErrorMessage = "Invalid company type. Must be 1 (Carrier) or 2 (Dispatch Service).")]
        public int CompanyType { get; init; }

        public int? ParentId { get; set; }

        public int? CategoryType { get; set; } = 1;

        public string Status { get; set; } = "Active";
    }
}