// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API;
using OKExV5Vendor.API.REST.JsonConverters;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket.Models;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExPlaceAlgoOrderRequest : OKExPlaceOrderBaseRequest<OKExAlgoOrderType>
    {
        [JsonProperty("orderPx")]
        public double? Price
        {
            get
            {
                if (this.price == null && this.TriggerPrice != null)
                    return OKExAlgoOrder.MARKET_PRICE_INDICATOR;
                else
                    return price;
            }
            internal set => price = value;
        }
        private double? price;
        [JsonProperty("triggerPx")]
        public double? TriggerPrice { get; internal set; }

        [JsonProperty("tpTriggerPx")]
        public double? TakeProfitTriggerPrice { get; internal set; }
        [JsonProperty("tpOrdPx")]
        public double? TakeProfitPrice
        {
            get
            {
                if (this.tpPrice == null && this.TakeProfitTriggerPrice != null)
                    return OKExAlgoOrder.MARKET_PRICE_INDICATOR;
                else
                    return tpPrice;
            }
            internal set => tpPrice = value;
        }
        private double? tpPrice;

        [JsonProperty("slTriggerPx")]
        public double? StopLossTriggerPrice { get; internal set; }
        [JsonProperty("slOrdPx")]
        public double? StopLossPrice
        {
            get
            {
                if (this.slPrice == null && this.StopLossTriggerPrice != null)
                    return OKExAlgoOrder.MARKET_PRICE_INDICATOR;
                else
                    return slPrice;
            }
            internal set => slPrice = value;
        }
        private double? slPrice;

        public OKExPlaceAlgoOrderRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, OKExAlgoOrderType orderType, string size)
            : base(symbol, tradeMode, side, orderType, size)
        { }
    }
}
