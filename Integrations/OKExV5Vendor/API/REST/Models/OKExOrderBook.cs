// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExOrderBook
    {
        [JsonConverter(typeof(JsonOrderBookConverter))]
        [JsonProperty("asks")]
        public OKExOrderBookItem[] Asks { get; set; }

        [JsonConverter(typeof(JsonOrderBookConverter))]
        [JsonProperty("bids")]
        public OKExOrderBookItem[] Bids { get; set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("ts")]
        internal long? _time;
        public DateTime Time => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;
    }

    struct OKExOrderBookItem
    {
        public double Price { get; set; }
        public double Size { get; set; }
    }
}
