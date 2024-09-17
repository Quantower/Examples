// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExTradingOrderResponce : OKExTradingResponce
{
    [JsonProperty("clOrdId")]
    public string ClientIrderId { get; set; }

    [JsonProperty("tag")]
    public string Tag { get; set; }

    [JsonProperty("ordId")]
    public override string OrderId { get; set; }
}