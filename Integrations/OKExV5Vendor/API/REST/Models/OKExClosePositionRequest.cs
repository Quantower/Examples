// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExClosePositionRequest
{
    [JsonProperty("instId")]
    public string InstrumentId { get; private set; }

    [JsonProperty("mgnMode")]
    public OKExTradeMode MarginMode { get; set; }

    [JsonProperty("ccy")]
    public string MarginCurrency { get; set; }

    [JsonProperty("posSide")]
    public OKExPositionSide? PositionSide { get; set; }

    public OKExClosePositionRequest(OKExSymbol oKExSymbol, OKExTradeMode marginMode)
    {
        this.InstrumentId = oKExSymbol.OKExInstrumentId;
        this.MarginMode = marginMode;
    }
}