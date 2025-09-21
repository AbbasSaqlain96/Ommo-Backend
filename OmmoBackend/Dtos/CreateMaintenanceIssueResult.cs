using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record CreateMaintenanceIssueResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; }
    }
}