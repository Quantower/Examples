// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using Newtonsoft.Json;

namespace BitfinexVendor.API
{
    class BitfinexSocketRequestResponse
    {
        [JsonProperty("event")]
        public BitfinexEvent Event { get; set; }

        [JsonProperty("channel")]
        public BitfinexChannelType Channel { get; set; }

        [JsonProperty("pair")]
        public string Pair { get; set; }

        [JsonProperty("chanId")]
        public int ChannelId { get; set; }

        [JsonProperty("len")]
        public int BookLength { get; set; }

        public override string ToString() => $"{this.Event} | {this.Channel} | {this.Pair} | {this.ChannelId}";
    }
}
