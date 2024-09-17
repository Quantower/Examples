// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.Websocket.Models;

class OKExFeeRate
{
    [JsonProperty("category")]
    public int? FeeSchedule { get; set; }

    [JsonProperty("level")]
    public string Level { get; set; }

    [JsonProperty("taker")]
    public double? Taker { get; set; }

    [JsonProperty("maker")]
    public double? Maker { get; set; }

    [JsonProperty("takerU")]
    public double? TakerUsdt { get; set; }

    [JsonProperty("makerU")]
    public double? MakerUsdt { get; set; }

    public bool IsVIP5orGreater => this.Level == "Lv5" || this.Level == "Lv6" || this.Level == "Lv7" || this.Level == "Lv8";
}