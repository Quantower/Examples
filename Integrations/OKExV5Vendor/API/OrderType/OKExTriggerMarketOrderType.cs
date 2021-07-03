using System;
using System.Collections.Generic;
using System.Text;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.OrderType
{
    class OKExTriggerMarketOrderType : StopOrderType
    {
        public const string ID = "Trigger";

        public override string Id => ID;
        public override string Name => ID;

        public OKExTriggerMarketOrderType(params TimeInForce[] allowedTimeInForce)
            : base(allowedTimeInForce)
        {
            
        }
    }
}
