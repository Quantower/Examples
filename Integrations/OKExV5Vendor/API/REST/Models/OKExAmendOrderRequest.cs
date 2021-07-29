using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExAmendOrderRequest
    {
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        [JsonProperty("newSz")]
        public string NewSize { get; set; }

        [JsonProperty("newPx")]
        public double? NewPrice { get; set; }

        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        public OKExAmendOrderRequest(OKExSymbol symbol, string orderId)
        {
            this.InstrumentId = symbol.OKExInstrumentId;
            this.OrderId = orderId;
        }
    }
}
