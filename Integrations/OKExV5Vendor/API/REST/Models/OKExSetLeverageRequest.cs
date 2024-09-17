// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExSetLeverageRequest
{
    [JsonProperty("instId")]
    public string InstrumentId { get; }

    [JsonProperty("ccy")]
    public string Currency { get; }

    [JsonProperty("lever")]
    public double Leverage { get; }

    [JsonProperty("mgnMode")]
    public OKExMarginMode MarginMode { get; }

    [JsonProperty("posSide")]
    public OKExPositionSide? PosSide { get; set; }

    private OKExSetLeverageRequest(double leverage, OKExMarginMode mode)
    {
        this.Leverage = leverage;
        this.MarginMode = mode;
    }
    public OKExSetLeverageRequest(OKExSymbol symbol, double leverage, OKExMarginMode mode)
        : this(leverage, mode)
    {
        this.InstrumentId = symbol.OKExInstrumentId;
    }
    public OKExSetLeverageRequest(string ccy, double leverage, OKExMarginMode mode)
        : this(leverage, mode)
    {
        this.Currency = ccy;
    }
}