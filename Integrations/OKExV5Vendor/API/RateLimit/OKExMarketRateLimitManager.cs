// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace OKExV5Vendor.API.RateLimit;

internal class OKExMarketRateLimitManager
{
    internal OKExRateLimitWaiter GetSymbols { get; }
    internal OKExRateLimitWaiter GetCandleHistory { get; }
    internal OKExRateLimitWaiter GetIndexCandleHistory { get; }
    internal OKExRateLimitWaiter GetTopCandleHistory { get; }
    internal OKExRateLimitWaiter GetMarkCandleHistory { get; }
    internal OKExRateLimitWaiter GetTicksHistory { get; }

    public OKExMarketRateLimitManager()
    {
        this.GetSymbols = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetCandleHistory = new OKExRateLimitWaiter(40, TimeSpan.FromSeconds(2));
        this.GetIndexCandleHistory = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetTopCandleHistory = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetMarkCandleHistory = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetTicksHistory = new OKExRateLimitWaiter(10, TimeSpan.FromSeconds(2));
    }
}