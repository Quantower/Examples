// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OKExV5Vendor.API.RateLimit;

internal class OKExRateLimitManager
{
    private static readonly object tradingRateLimitLocker = new();
    private static readonly IDictionary<string, OKExTradingRateLimitManager> tradingRateLimitsByUserId;

    internal static OKExMarketRateLimitManager Market { get; }
    internal static OKExTradingRateLimitManager Trading { get; }

    static OKExRateLimitManager()
    {
        Market = new OKExMarketRateLimitManager();
        Trading = new OKExTradingRateLimitManager();

        tradingRateLimitsByUserId = new ConcurrentDictionary<string, OKExTradingRateLimitManager>();
    }

    internal static OKExTradingRateLimitManager GetOrCreateTradingRateLimitManager(string userId)
    {
        lock (tradingRateLimitLocker)
        {
            if (!tradingRateLimitsByUserId.TryGetValue(userId, out var manager))
                tradingRateLimitsByUserId[userId] = manager = new OKExTradingRateLimitManager();

            return manager;
        }
    }
}
