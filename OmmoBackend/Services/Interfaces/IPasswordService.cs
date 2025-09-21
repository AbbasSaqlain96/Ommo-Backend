using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface IPasswordService
    {
        void HashPassword(string password, out byte[] passwordHash, out byte[] passwordSalt);
    }
}