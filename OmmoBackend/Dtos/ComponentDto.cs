using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record ComponentDto
    {
        public string ComponentName { get; init; }
        public int AccessLevel { get; init; }
    }
}