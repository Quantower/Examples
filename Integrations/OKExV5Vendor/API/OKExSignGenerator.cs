using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace OKExV5Vendor.API
{
    class OKExSignGenerator
    {
        private static string HmacSHA256(string infoStr, string secret)
        {
            byte[] sha256Data = Encoding.UTF8.GetBytes(infoStr);
            byte[] secretData = Encoding.UTF8.GetBytes(secret);
            using (var hmacsha256 = new HMACSHA256(secretData))
            {
                byte[] buffer = hmacsha256.ComputeHash(sha256Data);
                return Convert.ToBase64String(buffer);
            }
        }
        internal static string Generate(string timestamp, HttpMethod method, string endpoint, string secret)
        {
            return HmacSHA256($"{timestamp}{method.ToString().ToUpper()}{endpoint}", secret);
        }
    }
}
