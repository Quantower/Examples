// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.RateLimit;

internal class OKExRateLimitWaiter
{
    private readonly int requestCount;
    private readonly TimeSpan perTime;
    private readonly string uniqueId;

    private readonly double delayBetweenRequestsMs;

    private DateTime lastRequestTime;
    private readonly object lockObject = new();

    public OKExRateLimitWaiter(int requestCount, TimeSpan perTime, string uniqueId = null)
    {
        this.requestCount = requestCount;
        this.perTime = perTime;
        this.uniqueId = uniqueId;

        this.delayBetweenRequestsMs = perTime.TotalMilliseconds / requestCount;
    }

    public void WaitMyTurn(CancellationToken token)
    {
        lock (this.lockObject)
        {
#warning Не впевнений, що це хороша реалізація, але кращого поки що не придумав.

            var now = Core.Instance.TimeUtils.DateTimeUtcNow;
            var delta = now - this.lastRequestTime;

            if (delta.TotalMilliseconds < this.delayBetweenRequestsMs)
            {
                Task.Delay((int)this.delayBetweenRequestsMs, token).Wait(token);
                now = Core.Instance.TimeUtils.DateTimeUtcNow;
            }

            this.lastRequestTime = now;

            //if (this.uniqueId != null)
            //    System.Diagnostics.Trace.WriteLine($"[{now:O}] - ID: {this.uniqueId}\t Delay, ms: {this.delayBetweenRequestsMs}");
        }
    }
}