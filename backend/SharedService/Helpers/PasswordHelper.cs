using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Utils
{
    public static class PasswordHelper
    {
        // Returns salt.hash (base64.salt.base64.hash)
        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var subkey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
        }

        public static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split('.');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var expected = parts[1];
            var subkey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
            return Convert.ToBase64String(subkey) == expected;
        }
    }
}
