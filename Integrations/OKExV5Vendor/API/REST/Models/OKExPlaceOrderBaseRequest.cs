using Newtonsoft.Json;
using OKExV5Vendor.API;
using OKExV5Vendor.API.REST.Models;

namespace OKExV5Vendor.API.REST.Models
{
    abstract class OKExPlaceOrderBaseRequest<T1>
    {
        [JsonIgnore()]
        public OKExSymbol Symbol { get; private set; }

        [JsonProperty("instId")]
        public string InstrumentId => this.Symbol.OKExInstrumentId;

        [JsonProperty("tdMode")]
        public OKExTradeMode TradeMode { get; private set; }

        [JsonProperty("side")]
        public OKExSide Side { get; private set; }

        [JsonProperty("ordType")]
        public T1 OrderType { get; private set; }

        [JsonProperty("sz")]
        public string Size { get; private set; }

        [JsonProperty("ccy")]
        public string MarginCurrency { get; set; }

        [JsonProperty("posSide")]
        public OKExPositionSide? PositionSide { get; set; }

        [JsonProperty("reduceOnly")]
        public bool? ReduceOnly { get; set; }

        public OKExPlaceOrderBaseRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, T1 orderType, string size)
        {
            this.Symbol = symbol;
            this.TradeMode = tradeMode;
            this.Side = side;
            this.OrderType = orderType;
            this.Size = size;
        }
    }
}
