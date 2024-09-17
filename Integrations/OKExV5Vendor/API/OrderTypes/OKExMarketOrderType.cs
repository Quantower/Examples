// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace OKExV5Vendor.API.OrderTypes;

internal class OKExMarketOrderType : MarketOrderType
{
    public OKExMarketOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    {
    }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings)
    {
        var settings = base.GetOrderSettings(parameters, formatSettings);

        if (parameters.Type == RequestType.PlaceOrder)
        {
            OKExOrderTypeHelper.AddTradeMode(parameters.Symbol, settings);

            if (parameters.Symbol.SymbolType != SymbolType.Options)
            {
                if (parameters.Symbol.SymbolType == SymbolType.Crypto)
                    OKExOrderTypeHelper.AddReduceOnly(settings);
                else if (parameters.Symbol.SymbolType != SymbolType.Options)
                    OKExOrderTypeHelper.AddOrderBehaviour(settings);

                OKExOrderTypeHelper.AddComment(settings, string.Empty);
            }
        }

        return settings;
    }
}