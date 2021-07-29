using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExTransaction : OKExSymbolBasedObject, IPaginationLoadingItemWithTime
    {
        [JsonProperty("instType")]
        public override OKExInstrumentType InstrumentType { get; set; }

        [JsonProperty("instId")]
        public override string OKExInstrumentId { get; set; }

        [JsonProperty("tradeId")]
        public string TradeId { get; set; }
        public bool HasTradeId => this.TradeId != "0";

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

        DateTime IPaginationLoadingItemWithTime.Time => this.Time;
        string IPaginationLoadingItem.AfterId => this.BillId;

        #endregion IPaginationLoadingItemWithTime
    }
}
