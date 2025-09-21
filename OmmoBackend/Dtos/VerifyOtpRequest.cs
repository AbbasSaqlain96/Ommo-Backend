using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record VerifyOtpRequest
    {
        public int OtpId { get; set; }
        public int OtpNumber { get; set; }
    }
}