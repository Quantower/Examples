// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExIndexTicker : OKExTickerBase, IOKExIndexPrice
    {
        [JsonProperty("idxPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? IndexPrice { get; set; }

    }
}
