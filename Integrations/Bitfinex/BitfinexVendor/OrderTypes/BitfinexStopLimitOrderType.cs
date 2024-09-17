// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using BitfinexVendor.Extensions;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor.OrderTypes;

public class BitfinexStopLimitOrderType : StopLimitOrderType
{
    public BitfinexStopLimitOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    { }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings) =>
        base.GetOrderSettings(parameters, formatSettings)
            .AddReduceOnly(parameters, 100)
            .AddHidden(parameters, 110)
            .AddLeverage(parameters, 120)
            .AddClientOrderId(parameters, 130);

    public override ValidateResult ValidateOrderRequestParameters(OrderRequestParameters parameters)
    {
        var result = base.ValidateOrderRequestParameters(parameters);
        if (result.State != ValidateState.Valid)
            return result;

        result = this.ValidateIfMarginAllowed(parameters);
        if (result.State != ValidateState.Valid)
            return result;

        return ValidateResult.Valid;
    }
}