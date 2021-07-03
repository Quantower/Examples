using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExAccount
    {
        [JsonProperty("uid")]
        public string Id { get; set; }

        [JsonProperty("acctLv", ItemConverterType = typeof(JsonStringToIntOrNullConverter))]
        internal int? _accountLevel;
        public OKExAccountLevel AccountLevel => _accountLevel.HasValue ? (OKExAccountLevel)_accountLevel.Value : OKExAccountLevel.Simple;

        [JsonProperty("posMode")]
        public OKExPositionMode PositionMode { get; set; }

        [JsonProperty("autoLoan")]
        public bool AutoLoan { get; set; }

        [JsonProperty("greeksType")]
        public OKExGreeksType GreeksType { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("levelTmp")]
        public string LevelTmp { get; set; }
    }
}
