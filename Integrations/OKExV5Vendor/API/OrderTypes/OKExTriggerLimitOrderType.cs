// Copyright QUANTOWER LLC. � 2017-2022. All rights reserved.

using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.OrderTypes
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
