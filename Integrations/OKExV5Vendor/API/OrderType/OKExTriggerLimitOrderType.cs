using System;
using System.Collections.Generic;
using System.Text;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.OrderType
{
    class OKExTriggerLimitOrderType : StopLimitOrderType
    {
        public const string ID = "Trigger limit";

        public override string Id => ID; 
        public override string Name => ID; 

        public OKExTriggerLimitOrderType(params TimeInForce[] allowedTimeInForce)
            : base(allowedTimeInForce)
        {
        }
    }
}
