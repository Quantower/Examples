// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using OKExV5Vendor.API.REST.Models;
using System;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models
{
    [Obfuscation(Exclude = true)]
    class OKExOpenInterest : OKExSymbolBasedObject
    {
        [JsonProperty("instType")]
        public override OKExInstrumentType InstrumentType { get; set; }

        [JsonProperty("instId")]
        public override string OKExInstrumentId { get; set; }

        [JsonProperty("oi")]
        public double? OpenInterestInContracts { get; set; }

        [JsonProperty("oiCcy")]
        public double? OpenInterestInCurrency { get; set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("ts")]
        internal long? _ts;
        public DateTime FundingTime => this._ts.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this._ts.Value).UtcDateTime : default;

    }
}
