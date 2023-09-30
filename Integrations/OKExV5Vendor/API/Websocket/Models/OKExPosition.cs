// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using OKExV5Vendor.API.REST.Models;
using System;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExPosition : OKExSymbolBasedObject
{
    [JsonProperty("instType", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public override OKExInstrumentType InstrumentType { get; set; }

    [JsonProperty("instId")]
    public override string OKExInstrumentId { get; set; }

    [JsonProperty("mgnMode", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public OKExTradeMode MarginMode { get; set; }

    [JsonProperty("posId")]
    public string PositionId { get; set; }

    [JsonProperty("posSide", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public OKExPositionSide? PositionSide { get; set; }

    [JsonProperty("pos", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Quantity { get; set; }

    [JsonProperty("posCcy")]
    public string PositionCurrency { get; set; }

    [JsonProperty("availPos", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? QuantityCanBeClosed { get; set; }

    [JsonProperty("avgPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? AveragePrice { get; set; }

    [JsonProperty("upl", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? UnrealizedPnl { get; set; }

    [JsonProperty("uplRatio", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? UnrealizedPnlRatio { get; set; }

    [JsonProperty("lever", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Leverage { get; set; }

    [JsonProperty("liqPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? LiquidationPrice { get; set; }

    [JsonProperty("imr", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? InitialMarginRequirement { get; set; }

    [JsonProperty("margin", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Margin { get; set; }

    [JsonProperty("interest", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Interest { get; set; }

    [JsonProperty("tradeId")]
    public string TradeId { get; set; }

    [JsonProperty("notionalUsd", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? QuantityInUSD { get; set; }

    [JsonProperty("ccy")]
    public string MarginCurrency { get; set; }

    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("cTime")]
    internal long? _cTime;
    public DateTime CreationTime => this._cTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._cTime.Value).UtcDateTime : default;

    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("uTime")]
    internal long? _uTime;
    public DateTime UpdatedTime => this._uTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._uTime.Value).UtcDateTime : default;

    public bool IsClosed => !this.AveragePrice.HasValue;
}