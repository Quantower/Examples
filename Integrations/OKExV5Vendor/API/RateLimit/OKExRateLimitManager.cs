// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;

namespace OKExV5Vendor.API.RateLimit
{
    static class OKExRateLimitManager
    {
        public static OKExRateLimitWaiter TransactionDetails { get; } = new OKExRateLimitWaiter(10, TimeSpan.FromSeconds(2));
        public static OKExRateLimitWaiter OrdersHistory { get; } = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        public static OKExRateLimitWaiter AlgoOrdersHistory { get; } = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
    }
}
