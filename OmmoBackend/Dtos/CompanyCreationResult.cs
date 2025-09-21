using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record CompanyCreationResult
    {
        public int CompanyId { get; init; }
        public int RoleId { get; set; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}