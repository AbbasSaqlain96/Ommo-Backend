using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class PasswordService : IPasswordService
    {
        /// <summary>
        /// Hashes the password using HMACSHA512.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <param name="passwordHash">The resulting password hash.</param>
        /// <param name="passwordSalt">The resulting password salt.</param>

        public void HashPassword(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var h = new HMACSHA512())
            {
                passwordSalt = h.Key;
                passwordHash = h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }
}