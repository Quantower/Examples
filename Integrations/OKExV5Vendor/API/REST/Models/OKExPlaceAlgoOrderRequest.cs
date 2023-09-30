// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.Websocket.Models;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExPlaceAlgoOrderRequest : OKExPlaceOrderBaseRequest<OKExAlgoOrderType>
{
    [JsonProperty("orderPx")]
    public string Price
    {
        get
        {
            if (this.price == null && this.TriggerPrice != null)
                return OKExAlgoOrder.MARKET_PRICE_INDICATOR.ToString();
            else
                return price;
        }
        internal set => price = value;
    }
    private string price;
    [JsonProperty("triggerPx")]
    public string TriggerPrice { get; internal set; }

    [JsonProperty("tpTriggerPx")]
    public string TakeProfitTriggerPrice { get; internal set; }
    [JsonProperty("tpOrdPx")]
    public string TakeProfitPrice
    {
        get
        {
            if (this.tpPrice == null && this.TakeProfitTriggerPrice != null)
                return OKExAlgoOrder.MARKET_PRICE_INDICATOR.ToString();
            else
                return tpPrice;
        }
        internal set => tpPrice = value;
    }
    private string tpPrice;

    [JsonProperty("slTriggerPx")]
    public string StopLossTriggerPrice { get; internal set; }
    [JsonProperty("slOrdPx")]
    public string StopLossPrice
    {
        get
        {
            if (this.slPrice == null && this.StopLossTriggerPrice != null)
                return OKExAlgoOrder.MARKET_PRICE_INDICATOR.ToString();
            else
                return slPrice;
        }
        internal set => slPrice = value;
    }
    private string slPrice;

    public OKExPlaceAlgoOrderRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, OKExAlgoOrderType orderType, string size)
        : base(symbol, tradeMode, side, orderType, size)
    { }
}