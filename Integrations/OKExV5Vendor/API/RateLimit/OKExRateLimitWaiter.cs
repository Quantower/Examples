using System;
using System.Threading;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.RateLimit
{
    class OKExRateLimitWaiter
    {
        private readonly int requestCount;
        private readonly TimeSpan perTime;

        private int currentCount;
        private DateTime lastResetTime;

        public OKExRateLimitWaiter(int requestCount, TimeSpan perTime)
        {
            this.requestCount = requestCount;
            this.perTime = perTime;
        }

        public void Wait()
        {
            var now = Core.Instance.TimeUtils.DateTimeUtcNow;

            if (this.currentCount > this.requestCount)
            {
                var deltaTime = now - this.lastResetTime;

                if (deltaTime < this.perTime)
                    Thread.Sleep(this.perTime - deltaTime);

                this.currentCount = 0;
                this.lastResetTime = now;
            }
            else
            {
                var deltaTime = now - this.lastResetTime;

                if (deltaTime > this.perTime)
                {
                    this.currentCount = 0;
                    this.lastResetTime = now;
                }
            }

            this.currentCount++;
        }
    }
}
