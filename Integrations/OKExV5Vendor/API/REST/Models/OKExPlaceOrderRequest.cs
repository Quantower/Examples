using Newtonsoft.Json;
using OKExV5Vendor.API;
using OKExV5Vendor.API.REST.Models;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExPlaceOrderRequest : OKExPlaceOrderBaseRequest<OKExOrderType>
    {
        [JsonProperty("px")]
        public double? Price { get; set; }

        public OKExPlaceOrderRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, OKExOrderType orderType, string size)
            : base(symbol, tradeMode, side, orderType, size)
        { }
    }
}
