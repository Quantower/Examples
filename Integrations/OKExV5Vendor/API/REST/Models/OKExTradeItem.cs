// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExTradeItem : IPaginationLoadingItem
{
    [JsonProperty("instId")]
    public string InstrumentId { get; set; }

    [JsonProperty("tradeId")]
    public string TradeId { get; set; }

    [JsonProperty("px", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Price { get; set; }

    [JsonProperty("sz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Size { get; set; }

    [JsonProperty("side")]
    public OKExSide? Side { get; set; }

    [JsonProperty("ts", ItemConverterType = typeof(JsonStringToLongOrDefaultConverter))]
    public long? _time;

    public DateTime Time => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;

    public string AfterId => null;
}