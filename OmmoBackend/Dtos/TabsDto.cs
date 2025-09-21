using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record TabsDto
    {
        public string ModuleName { get; set; }
        public int AccessLevel { get; set; }
        public List<ComponentDto> Components { get; set; }
    }
}