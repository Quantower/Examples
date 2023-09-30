// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExIndexTicker : OKExTickerBase, IOKExIndexPrice
{
    [JsonProperty("idxPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? IndexPrice { get; set; }
}