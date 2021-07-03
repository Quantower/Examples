using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExMarkItem
    {
        [JsonProperty("instType")]
        public OKExInstrumentType InstrumentType { get; set; }

        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        [JsonProperty("markPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? MarkPrice { get; set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("ts")]
        internal long? _time;
        public DateTime Time => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;
    }
}
