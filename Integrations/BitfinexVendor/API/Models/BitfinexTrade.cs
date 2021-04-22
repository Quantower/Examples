// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using BitfinexVendor.API.JsonConverters;
using Newtonsoft.Json;

namespace BitfinexVendor.API.Models
{
    [JsonConverter(typeof(BitfinexTradeJsonConverter))]
    class BitfinexTrade
    {
        public string Pair { get; internal set; }

        public long Timestamp { get; internal set; }

        public decimal Price { get; internal set; }

        public decimal Amount { get; internal set; }

        public override string ToString() => $"{this.Timestamp} | {this.Price} | {this.Amount}";
    }
}
