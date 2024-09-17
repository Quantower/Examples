// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using OKExV5Vendor.API.REST.Models;
using System;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExFundingRate : OKExSymbolBasedObject
{
    [JsonProperty("instType")]
    public override OKExInstrumentType InstrumentType { get; set; }

    [JsonProperty("instId")]
    public override string OKExInstrumentId { get; set; }

    [JsonProperty("fundingRate")]
    public double? FundingRate { get; set; }

    [JsonProperty("nextFundingRate")]
    public double? NextFundingRate { get; set; }

    [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
    [JsonProperty("fundingTime")]
    internal long? _fundingTime;
    public DateTime FundingTime => this._fundingTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._fundingTime.Value).UtcDateTime : default;
}