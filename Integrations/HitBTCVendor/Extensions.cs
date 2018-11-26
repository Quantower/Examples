using HitBTC.Net.Models;

namespace HitBTCVendor
{
    static class Extensions
    {
        public static string Format(this HitError hitError) => $"{hitError.Code}. {hitError.Message}. {hitError.Description}";
    }
}