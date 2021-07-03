using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExPlaceAlgoOrderResponce : OKExTradingResponce
    {
        [JsonProperty("algoId")]
        public override string OrderId { get; set; }
    }
}
