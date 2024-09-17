// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal abstract class OKExTradingResponce
{
    public abstract string OrderId { get; set; }

    [JsonProperty("sCode")]
    public string Code { get; set; }

    [JsonProperty("sMsg")]
    public string Message { get; set; }

    public bool IsSuccess => this.Code == "0";
}