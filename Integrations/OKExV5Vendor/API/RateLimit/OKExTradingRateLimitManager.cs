// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;

namespace OKExV5Vendor.API.RateLimit;

internal class OKExTradingRateLimitManager
{
    internal OKExRateLimitWaiter SetLeverage { get; }
    internal OKExRateLimitWaiter GetLeverage { get; }
    internal OKExRateLimitWaiter OrdersHistory { get; }
    internal OKExRateLimitWaiter AlgoOrdersHistory { get; }
    internal OKExRateLimitWaiter GetAccount { get; }
    internal OKExRateLimitWaiter GetFeeRate { get; }
    internal OKExRateLimitWaiter GetOrdersList { get; }
    internal OKExRateLimitWaiter GetAlgoOrdersList { get; }
    internal OKExRateLimitWaiter GetPositions { get; }
    internal OKExRateLimitWaiter TransactionDetails { get; }

    public OKExTradingRateLimitManager()
    {
        this.TransactionDetails = new OKExRateLimitWaiter(10, TimeSpan.FromSeconds(2));
        this.SetLeverage = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetLeverage = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.OrdersHistory = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.AlgoOrdersHistory = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetAccount = new OKExRateLimitWaiter(5, TimeSpan.FromSeconds(2));
        this.GetFeeRate = new OKExRateLimitWaiter(5, TimeSpan.FromSeconds(2));
        this.GetOrdersList = new OKExRateLimitWaiter(60, TimeSpan.FromSeconds(2));
        this.GetAlgoOrdersList = new OKExRateLimitWaiter(20, TimeSpan.FromSeconds(2));
        this.GetPositions = new OKExRateLimitWaiter(10, TimeSpan.FromSeconds(2));
    }
}
