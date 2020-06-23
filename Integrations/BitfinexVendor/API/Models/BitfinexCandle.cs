// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using BitfinexVendor.API.JsonConverters;
using Newtonsoft.Json;

namespace BitfinexVendor.API.Models
{
    [JsonConverter(typeof(BitfinexCandleJsonConverter))]
    class BitfinexCandle
    {
        public long Timestamp { get; internal set; }

        public decimal Open { get; internal set; }

        public decimal Close { get; internal set; }

        public decimal High { get; internal set; }

        public decimal Low { get; internal set; }

        public decimal Volume { get; internal set; }

        public override string ToString() => $"{this.Timestamp} | O: {this.Open} | H: {this.High} | L: {this.Low} | C: {this.Close} | V: {this.Volume}";
    }
}
