// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExBalanceItem
{
    [JsonProperty("availBal", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? AvailableBalance { get; set; }

    [JsonProperty("availEq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? AvailableEquity { get; set; }

    [JsonProperty("cashBal", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? CashBalance { get; set; }

    [JsonProperty("ccy")]
    public string Currency { get; set; }

    [JsonProperty("eq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Equity { get; set; }

    [JsonProperty("isoEq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? IsolatedEquity { get; set; }

    [JsonProperty("disEq", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? DiscountEquity { get; set; }

    [JsonProperty("frozenBal", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? FrozenBalance { get; set; }

    [JsonProperty("ordFrozen", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? OrderFrozenMargin { get; set; }

    [JsonProperty("upl", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? UnrealizedPL { get; set; }

    [JsonProperty("mgnRatio", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? MarginRatio { get; set; }

    [JsonProperty("eqUsd", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? EquityUSD { get; set; }
}