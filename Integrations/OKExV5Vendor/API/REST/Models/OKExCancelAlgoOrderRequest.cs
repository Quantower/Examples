using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExCancelAlgoOrderRequest
    {
        [JsonProperty("algoId")]
        public string AlgoOrderId { get; set; }

        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        public OKExCancelAlgoOrderRequest(OKExSymbol oKExSymbol, string algoOrderId)
        {
            this.InstrumentId = oKExSymbol.OKExInstrumentId;
            this.AlgoOrderId = algoOrderId;
        }
    }
}
