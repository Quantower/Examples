// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExLeverage
{
    [JsonProperty("instId")]
    public string InstrumentId { get; set; }

    [JsonProperty("mgnMode")]
    public OKExTradeMode? TradeMode { get; set; }

    [JsonProperty("posSide")]
    public OKExPositionSide? PositionSide { get; set; }

    [JsonProperty("lever")]
    public double? Leverage { get; set; }
}