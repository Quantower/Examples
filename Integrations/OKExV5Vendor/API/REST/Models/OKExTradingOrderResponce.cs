// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExTradingOrderResponce : OKExTradingResponce
    {
        [JsonProperty("clOrdId")]
        public string ClientIrderId { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("ordId")]
        public override string OrderId { get; set; }
    }
}
