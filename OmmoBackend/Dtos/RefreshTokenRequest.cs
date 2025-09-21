using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record RefreshTokenRequest
    {
        public string RefreshToken { get; init; }   
    }
}