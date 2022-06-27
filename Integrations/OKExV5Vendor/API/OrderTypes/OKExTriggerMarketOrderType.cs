// Copyright QUANTOWER LLC. � 2017-2022. All rights reserved.

using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.OrderTypes
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
