using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.REST.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExWithdrawalRecord : IPaginationLoadingItem
    {
        [JsonProperty("txId")]
        public string HashRecord { get; internal set; }

        [JsonProperty("wdId")]
        public string WithdrawalId { get; internal set; }

        [JsonProperty("ccy")]
        public string Currency { get; internal set; }

        [JsonProperty("chain")]
        public string Chain { get; internal set; }

        [JsonProperty("amt", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? TokenAmount { get; internal set; }

        [JsonProperty("fee", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? Fee { get; internal set; }

        [JsonProperty("state", ItemConverterType = typeof(JsonStringToIntOrNullConverter))]
        internal int? _state;
        public OKExWithdrawalState State => this._state.HasValue ? (OKExWithdrawalState)this._state.Value : OKExWithdrawalState.Undefined;

        [JsonProperty("from")]
        public string From { get; internal set; }

        [JsonProperty("to")]
        public string To { get; internal set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("ts")]
        internal long? _time;
        public DateTime CreationTime => this._time.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._time.Value).UtcDateTime : default;

        #region IPaginationLoadingItem

        DateTime IPaginationLoadingItem.Time => this.CreationTime;
        string IPaginationLoadingItem.AfterId => this._time.ToString();

        #endregion IPaginationLoadingItem
    }
}
