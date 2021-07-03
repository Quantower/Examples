using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.REST.JsonConverters;
using OKExV5Vendor.API.REST.Models;
using System;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExAlgoOrder : OKExSymbolBasedObject, IPaginationLoadingItemWithTime
    {
        internal const int MARKET_PRICE_INDICATOR = -1;

        [JsonProperty("instType")]
        public override OKExInstrumentType InstrumentType { get; set; }

        [JsonProperty("instId")]
        public override string OKExInstrumentId { get; set; }

        [JsonProperty("ccy")]
        public string MarginCurrency { get; set; }

        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        [JsonProperty("algoId")]
        public string AlgoOrderId { get; set; }

        [JsonProperty("sz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? Size { get; set; }

        [JsonProperty("ordType", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
        public OKExAlgoOrderType AlgoOrderType { get; set; }

        [JsonProperty("side", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
        public OKExSide Side { get; set; }

        [JsonProperty("posSide", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
        public OKExPositionSide? PositionSide { get; set; }

        [JsonProperty("tdMode", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
        public OKExTradeMode TradeMode { get; set; }

        [JsonProperty("state", ItemConverterType = typeof(JsonStringToEnumOrDefaultConverter))]
        public OKExAlgoOrderState? State { get; set; }

        [JsonProperty("lever", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? Leverage { get; set; }

        [JsonProperty("tpTriggerPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? TakeProfitTriggerPrice { get; set; }

        [JsonProperty("tpOrdPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? TakeProfitPrice { get; set; }

        [JsonProperty("slTriggerPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? StopLossTriggerPrice { get; set; }

        [JsonProperty("slOrdPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? StopLossPrice { get; set; }

        [JsonProperty("triggerPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? TriggerPrice { get; set; }

        [JsonProperty("ordPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? Price { get; set; }

        [JsonProperty("actualSz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? ActualSize { get; set; }

        [JsonProperty("actualPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? ActualPrice { get; set; }

        [JsonProperty("actualSide")]
        public string ActualSide { get; set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("triggerTime")]
        internal long? _triggerTime;
        public DateTime TriggerTime => this._triggerTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._triggerTime.Value).UtcDateTime : default;

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("cTime")]
        internal long? _cTime;
        public DateTime CreationTime => this._cTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._cTime.Value).UtcDateTime : default;

        public bool HasStopLossPrice => this.StopLossPrice.HasValue && this.StopLossPrice != MARKET_PRICE_INDICATOR;
        public bool HasTakeProfitPrice => this.TakeProfitPrice.HasValue && this.TakeProfitPrice != MARKET_PRICE_INDICATOR;
        public bool HasPrice => this.Price.HasValue && this.Price != MARKET_PRICE_INDICATOR;

        public bool IsCancelling { get; internal set; }

        #region IPaginationLoadingItem

        string IPaginationLoadingItem.AfterId => this.AlgoOrderId;
        DateTime IPaginationLoadingItemWithTime.Time => this.CreationTime;

        #endregion IPaginationLoadingItem
    }
}
