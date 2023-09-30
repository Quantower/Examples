// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal abstract class OKExPlaceOrderBaseRequest
{
    [JsonIgnore()]
    public OKExSymbol Symbol { get; private set; }

    [JsonProperty("instId")]
    public string InstrumentId => this.Symbol.OKExInstrumentId;

    [JsonProperty("tdMode")]
    public OKExTradeMode TradeMode { get; private set; }

    [JsonProperty("side")]
    public OKExSide Side { get; private set; }

    [JsonProperty("sz")]
    public string Size { get; set; }

    [JsonProperty("ccy")]
    public string MarginCurrency { get; set; }

    [JsonProperty("posSide")]
    public OKExPositionSide? PositionSide { get; set; }

    [JsonProperty("reduceOnly")]
    public bool? ReduceOnly { get; set; }

    [JsonProperty("tgtCcy")]
    public OKExOrderQuantityType? QuantityType { get; set; }

    public OKExPlaceOrderBaseRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, string size)
    {
        this.Symbol = symbol;
        this.TradeMode = tradeMode;
        this.Side = side;
        this.Size = size;
    }

}

internal abstract class OKExPlaceOrderBaseRequest<TOrderType> : OKExPlaceOrderBaseRequest
{
    [JsonProperty("ordType")]
    public TOrderType OrderType { get; private set; }

    public OKExPlaceOrderBaseRequest(OKExSymbol symbol, OKExTradeMode tradeMode, OKExSide side, TOrderType orderType, string size)
        : base(symbol, tradeMode, side, size)
    {
        this.OrderType = orderType;
    }
}