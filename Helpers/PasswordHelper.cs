using System;
using System.Security.Cryptography;

namespace TourwebsiteFYP.Helpers
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            using (var rfc = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(rfc.Salt) + "." + Convert.ToBase64String(rfc.GetBytes(KeySize));
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || !storedHash.Contains("."))
                return false;

            var parts = storedHash.Split('.');
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            using (var rfc = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] actualHash = rfc.GetBytes(expectedHash.Length);
                return FixedTimeEquals(actualHash, expectedHash);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}