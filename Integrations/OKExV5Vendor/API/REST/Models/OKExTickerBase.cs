// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models
{
    abstract class OKExTickerBase
    {
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        [JsonProperty("open24h", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? OpenPrice24h { get; set; }

        [JsonProperty("sodUtc0", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? OpenPriceUTC0 { get; set; }

        [JsonProperty("high24h", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? HighPrice24h { get; set; }

        [JsonProperty("low24h", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? LowPrice24h { get; set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("ts")]
        internal long? _time;
        public DateTime Time => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;
    }
}
