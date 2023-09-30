// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExPlaceOrderRequest : OKExPlaceOrderBaseRequest<OKExOrderType>
{
    [JsonProperty("px")]
    public string Price { get; set; }

    [JsonProperty("tag")]
    public string Tag { get; set; }

    [JsonProperty("clOrdId")]
    public string ClientOrderId { get; set; }

    public OKExPlaceOrderRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, OKExOrderType orderType)
        : base(symbol, tradeMode, side, orderType, string.Empty)
    { }
}