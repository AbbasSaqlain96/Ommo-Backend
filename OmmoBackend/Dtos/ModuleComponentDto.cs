using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record ModuleComponentDto
    {
        public string ModuleName { get; init; }
        public string ComponentName { get; init; }
        public string AccessLevel { get; set; }
        public int ComponentAccessLevel { get; set; }
    }
}