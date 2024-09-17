// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExDepositRecord : IPaginationLoadingItem
{
    [JsonProperty("txId")]
    public string HashRecord { get; internal set; }

    [JsonProperty("depId")]
    public string DepositId { get; internal set; }

    [JsonProperty("ccy")]
    public string Currency { get; internal set; }

    [JsonProperty("chain")]
    public string Chain { get; internal set; }

    [JsonProperty("amt", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
    public double? DepositAmount { get; internal set; }

    [JsonProperty("state", ItemConverterType = typeof(JsonStringToIntOrNullConverter))]
    internal int? _state;
    public OKExDepositState State => this._state.HasValue ? (OKExDepositState)this._state.Value : OKExDepositState.WaitingForConfirm;

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