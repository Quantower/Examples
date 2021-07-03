using Newtonsoft.Json;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExChannelRequest
    {
        [JsonProperty("instType")]
        public OKExInstrumentType? InstrumentType { get; set; }

        [JsonProperty("channel")]
        public string ChannelName { get; set; }

        [JsonProperty("instId")]
        public string InstrumentId { get; set; }
    }
}
