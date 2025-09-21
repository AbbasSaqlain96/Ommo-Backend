using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record OtpRequest
    {
        public string receiver { get; set; }
        public string? Subject { get; set; }
    }
}