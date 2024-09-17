// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExPlaceAlgoOrderResponce : OKExTradingResponce
{
    [JsonProperty("algoId")]
    public override string OrderId { get; set; }
}