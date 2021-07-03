using System;
using System.Collections.Generic;
using System.Text;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace OKExV5Vendor.API.OrderType
{
    class OKExLimitOrderType : LimitOrderType
    {
        public OKExLimitOrderType(params TimeInForce[] allowedTimeInForce)
            : base(allowedTimeInForce)
        {
        }

        public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings)
        {
            var settings = base.GetOrderSettings(parameters, formatSettings);

            if (parameters.Type == RequestType.PlaceOrder)
            {
                if (parameters.Symbol.SymbolType != SymbolType.Options)
                    OKExOrderTypeHelper.AddTradeMode(parameters, settings);

                if (parameters.Symbol.SymbolType == SymbolType.Crypto)
                    OKExOrderTypeHelper.AddReduceOnly(settings);

                OKExOrderTypeHelper.AddPostOnly(settings);
            }

            return settings;
        }
    }
}
