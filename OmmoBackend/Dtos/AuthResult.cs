using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record AuthResult
    {
        public bool Success { get; init; }
        public string? Token { get; init; }
        public string? ErrorMessage { get; init; }
        public string RefreshToken { get; set; }
    }
}