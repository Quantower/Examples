// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExBalance
{
    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("uTime")]
    internal long? _time;
    public DateTime Time => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;

    [JsonProperty("totalEq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? TotalEquity { get; set; }

    [JsonProperty("isoEq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? IsolatedEquity { get; set; }

    [JsonProperty("adjEq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? AdjustedEquity { get; set; }

    [JsonProperty("ordFroz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? PendingOrderFrozenMargin { get; set; }

    [JsonProperty("imr", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? InitialMarginRequirement { get; set; }

    [JsonProperty("mmr", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? MaintenanceMarginRequirement { get; set; }

    [JsonProperty("mgnRatio", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? MarginRatio { get; set; }

    [JsonProperty("notionalUsd", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? PositionsQuantity { get; set; }

    [JsonProperty("details")]
    public OKExBalanceItem[] Details { get; set; }
}