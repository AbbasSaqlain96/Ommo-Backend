using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record TrailerInfoDto
    {
        public TrailerDto Trailer { get; init; }
        public TrailerLocationDto TrailerLocation { get; init; }
    }
}