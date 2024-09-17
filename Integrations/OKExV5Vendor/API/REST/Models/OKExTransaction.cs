// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using System;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExTransaction : OKExSymbolBasedObject, IPaginationLoadingItem
{
    [JsonProperty("instType")]
    public override OKExInstrumentType InstrumentType { get; set; }

    [JsonProperty("instId")]
    public override string OKExInstrumentId { get; set; }

    [JsonProperty("tradeId")]
    public string TradeId { get; set; }
    public bool HasTradeId => this.TradeId != "0";
    public string UniqueTradeId
    {
        get
        {
            if (this.HasTradeId)
                return $"{this.TradeId}_{this.InstrumentType.GetEnumMember().ToLowerInvariant()}";
            else
                return this.TradeId;
        }
    }

    [JsonProperty("ordId")]
    public string OrderId { get; set; }

    [JsonProperty("clOrdId")]
    public string ClientOrderId { get; set; }

    [JsonProperty("billId")]
    public string BillId { get; set; }

    [JsonProperty("fillPx")]
    public double? FillPrice { get; set; }

    [JsonProperty("fillSz")]
    public double? FillSize { get; set; }

    [JsonProperty("side")]
    public OKExSide Side { get; set; }

    [JsonProperty("posSide")]
    public OKExPositionSide PositionSide { get; set; }

    [JsonProperty("execType")]
    internal string orderFlowType;
    public bool IsTaker => this.orderFlowType == "T";
    public bool IsMaker => this.orderFlowType == "M";

    [JsonProperty("feeCcy")]
    public string FeeCurrency { get; set; }

    [JsonProperty("fee")]
    public double? Fee { get; set; }

    [JsonProperty("ts")]
    internal long? _time;
    public DateTime Time => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;

    #region IPaginationLoadingItemWithTime

    DateTime IPaginationLoadingItem.Time => this.Time;
    string IPaginationLoadingItem.AfterId => this.BillId;

    #endregion IPaginationLoadingItemWithTime
}