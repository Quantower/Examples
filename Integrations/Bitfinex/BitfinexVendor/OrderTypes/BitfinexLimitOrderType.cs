// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using BitfinexVendor.Extensions;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor.OrderTypes;

public class BitfinexLimitOrderType : LimitOrderType
{
    public BitfinexLimitOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    { }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings) =>
        base.GetOrderSettings(parameters, formatSettings)
            .AddReduceOnly(parameters, 100)
            .AddPostOnly(parameters, 110)
            .AddHidden(parameters, 120)
            .AddOco(parameters, 130)
            .AddOcoStopPrice(parameters, 140)
            .AddLeverage(parameters, 150)
            .AddClientOrderId(parameters, 160);

    public override void SetDefaultPrices(SettingItem[] settings, OrderRequestParameters parameters)
    {
        base.SetDefaultPrices(settings, parameters);

        double price = parameters.Side == Side.Buy ? parameters.Symbol.Bid : parameters.Symbol.Ask;
        if (double.IsNaN(price))
            return;

        settings.UpdateItemValue(BitfinexVendor.OCO_STOP_PRICE, price);
    }

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