using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models
{
    abstract class OKExTradingResponce
    {
        public abstract string OrderId { get; set; }

        [JsonProperty("sCode")]
        public string Code { get; set; }

        [JsonProperty("sMsg")]
        public string Message { get; set; }

        public bool IsSuccess => this.Code == "0";
    }
}
