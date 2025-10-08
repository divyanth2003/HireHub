using System;
using System.Security.Cryptography;
using System.Text;

namespace HireHub.API.Utils
{
    public static class TokenUtils
    {
        public static string CreateRawTokenBase64Url(int bytes = 32)
        {
            var data = RandomNumberGenerator.GetBytes(bytes);
            return Base64UrlEncode(data);
        }

        public static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string Sha256Hex(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
