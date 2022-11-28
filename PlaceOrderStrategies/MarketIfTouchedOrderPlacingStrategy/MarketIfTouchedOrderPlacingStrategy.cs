// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using TradingPlatform.BusinessLayer;

namespace PlaceOrderIfTouchedOrderPlacingStrategy
{
    public class MarketIfTouchedOrderPlacingStrategy : PlaceOrderIfTouchedOrderPlacingStrategy
    {
        public MarketIfTouchedOrderPlacingStrategy()
            : base()
        {
            this.Name = "Market if Touched";
            this.ShortName = "MIT";
        }

        protected override void OnPlaceOrder(PlaceOrderRequestParameters placeOrderRequest)
        {
            if (placeOrderRequest.OrderTypeId != OrderType.Limit)
                throw new ArgumentException("Only Limit order type is supported for Market if Touched");

            base.OnPlaceOrder(placeOrderRequest);
        }
        protected override void CustomizeOrderRequest(PlaceOrderRequestParameters placeOrderRequest)
        {
            placeOrderRequest.OrderTypeId = OrderType.Market;
        }
    }
}
