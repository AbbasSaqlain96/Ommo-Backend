using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record ForgetPasswordRequest
    {
        [Required(ErrorMessage = "Email or phone is required")]
        public string Identifier { get; init; }
        
        [Required(ErrorMessage = "New password is required")]
        public string NewPassword { get; init; }
    }
}