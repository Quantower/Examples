// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using BitfinexVendor.Extensions;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor.OrderTypes;

public class BitfinexFillOrKillOrderType : LimitOrderType
{
    public override string Id => BitfinexVendor.FILL_OR_KILL;

    public override string Name => BitfinexVendor.FILL_OR_KILL;

    public BitfinexFillOrKillOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    { }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings) =>
        base.GetOrderSettings(parameters, formatSettings)
            .AddReduceOnly(parameters, 100)
            .AddLeverage(parameters, 110)
            .AddClientOrderId(parameters, 120);

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