// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.REST.JsonConverters;
using OKExV5Vendor.API.REST.Models;
using System;
using System.Reflection;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExOrder : OKExSymbolBasedObject, IPaginationLoadingItem
{
    [JsonProperty("instType", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public override OKExInstrumentType InstrumentType { get; set; }

    [JsonProperty("instId")]
    public override string OKExInstrumentId { get; set; }

    [JsonProperty("ccy")]
    public string Currency { get; set; }

    [JsonProperty("ordId")]
    public string OrderId { get; set; }

    [JsonProperty("clOrdId")]
    public string ClientOrderId { get; set; }

    [JsonProperty("tag")]
    public string OrderTag { get; set; }

    [JsonProperty("tgtCcy")]
    public string TgtCurrency { get; set; }
    public bool IsTgtEqualToBaseCurrency => this.TgtCurrency == "base_ccy";

    [JsonProperty("px", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Price { get; set; }

    [JsonProperty("sz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Size { get; set; }

    [JsonProperty("pnl", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? PnL { get; set; }

    [JsonProperty("ordType", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public OKExOrderType OrderType { get; set; }

    [JsonProperty("side", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public OKExSide Side { get; set; }

    [JsonProperty("posSide", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public OKExPositionSide? PositionSide { get; set; }

    [JsonProperty("tdMode", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
    public OKExTradeMode TradeMode { get; set; }

    [JsonProperty("accFillSz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? AccumulatedFillQty { get; set; }

    [JsonProperty("fillPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? LastFilledPrice { get; set; }

    [JsonProperty("tradeId", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? LastTradeId { get; set; }
    public string UniqueLastTradeId => $"{this.LastTradeId}_{this.InstrumentType.GetEnumMember().ToLowerInvariant()}";

    [JsonProperty("fillSz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? LastFilledQty { get; set; }

    [JsonProperty("fillFee", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? LastFilledFee { get; set; }

    [JsonProperty("fillFeeCcy", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public string LastFilledFeeCurrency { get; set; }

    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("fillTime")]
    internal long? _fillTime;
    public DateTime LastFilledTime => this._fillTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._fillTime.Value).UtcDateTime : default;

    [JsonProperty("avgPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? AverageFilledPrice { get; set; }

    [JsonConverter(typeof(JsonStringToEnumOrDefaultConverter))]
    [JsonProperty("state")]
    public OKExOrderState State { get; set; }

    [JsonProperty("lever", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Leverage { get; set; }

    [JsonProperty("tpTriggerPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? TakeProfitTriggerPrice { get; set; }

    [JsonProperty("tpOrdPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? TakeProfitOrderPrice { get; set; }

    [JsonProperty("slTriggerPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? StopLossTriggerPrice { get; set; }

    [JsonProperty("slOrdPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? TStopLossOrderPrice { get; set; }

    [JsonProperty("feeCcy")]
    public string FeeCurrency { get; set; }

    [JsonProperty("fee", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? Fee { get; set; }

    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("uTime")]
    internal long? _uTime;
    public DateTime UpdateTime => this._uTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._uTime.Value).UtcDateTime : default;

    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("cTime")]
    internal long? _cTime;
    public DateTime CreationTime => this._cTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._cTime.Value).UtcDateTime : default;

    #region IPaginationLoadingItem

    string IPaginationLoadingItem.AfterId => this.OrderId;
    DateTime IPaginationLoadingItem.Time => this.CreationTime;

    #endregion IPaginationLoadingItem
}