using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record UserDto
    {
        public string Username { get; init; }
        public string Email { get; init; }
        public string Phone { get; init; }
        public int CompanyId { get; init; }
        public string RoleName { get; init; }
        public string ProfileImageUrl { get; init; }
    }
}