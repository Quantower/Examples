// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models
{
    [Obfuscation(Exclude = true)]
    class OKExChannelRequest
    {
        [JsonProperty("instType")]
        public OKExInstrumentType? InstrumentType { get; set; }

        [JsonProperty("channel")]
        public string ChannelName { get; set; }

        [JsonProperty("instId")]
        public string InstrumentId { get; set; }
    }
}
