using System;

namespace OKExV5Vendor.API.RateLimit
{
    static class OKExRateLimitManager
    {
        public static OKExRateLimitWaiter TransactionDetails { get; } = new OKExRateLimitWaiter(10, TimeSpan.FromSeconds(2));
    }
}
