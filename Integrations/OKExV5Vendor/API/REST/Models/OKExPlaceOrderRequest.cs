// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExPlaceOrderRequest : OKExPlaceOrderBaseRequest<OKExOrderType>
    {
        [JsonProperty("px")]
        public double? Price { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        public OKExPlaceOrderRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, OKExOrderType orderType, string size)
            : base(symbol, tradeMode, side, orderType, size)
        { }
    }
}
