using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record UserCreationResult
    {
        public bool Success { get; init; }
        
        public string ErrorMessage { get; init; }

        public int UserId { get; set; }
    }
}