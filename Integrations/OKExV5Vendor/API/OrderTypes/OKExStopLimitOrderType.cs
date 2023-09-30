// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace OKExV5Vendor.API.OrderTypes;

internal class OKExStopLimitOrderType : StopLimitOrderType
{
    public OKExStopLimitOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    {
    }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings)
    {
        var settings = base.GetOrderSettings(parameters, formatSettings);

        if (parameters.Symbol.SymbolType != SymbolType.Options)
        {
            OKExOrderTypeHelper.AddTradeMode(parameters.Symbol, settings);

            if (parameters.Symbol.SymbolType == SymbolType.Crypto)
                OKExOrderTypeHelper.AddReduceOnly(settings);
            else if (parameters.Symbol.SymbolType != SymbolType.Options)
                OKExOrderTypeHelper.AddOrderBehaviour(settings);
        }

        return settings;
    }
}