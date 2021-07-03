using Newtonsoft.Json;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExLoginArguments
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty("passphrase")]
        public string Passphrase { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("sign")]
        public string Sigh { get; set; }
    }
}
