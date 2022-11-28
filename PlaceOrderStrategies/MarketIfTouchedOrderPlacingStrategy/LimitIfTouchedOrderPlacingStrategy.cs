// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using TradingPlatform.BusinessLayer;

namespace PlaceOrderIfTouchedOrderPlacingStrategy
{
    public class LimitIfTouchedOrderPlacingStrategy : PlaceOrderIfTouchedOrderPlacingStrategy
    {
        public LimitIfTouchedOrderPlacingStrategy()
            : base()
        {
            this.Name = "Limit if Touched";
            this.ShortName = "LIT";
        }

        protected override void OnPlaceOrder(PlaceOrderRequestParameters placeOrderRequest)
        {
            if (placeOrderRequest.OrderTypeId != OrderType.Limit)
                throw new ArgumentException("Only Limit order type is supported for Limit if Touched");

            base.OnPlaceOrder(placeOrderRequest);
        }
        protected override void CustomizeOrderRequest(PlaceOrderRequestParameters placeOrderRequest)
        {
            placeOrderRequest.OrderTypeId = OrderType.Limit;
        }
    }
}
