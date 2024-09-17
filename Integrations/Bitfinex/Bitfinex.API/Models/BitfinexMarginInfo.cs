// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

namespace Bitfinex.API.Models;

public class BitfinexMarginInfo
{
    public decimal? UserPnL { get; internal set; }

    public decimal? UserSwaps { get; internal set; }

    public decimal? MarginBalance { get; internal set; }

    public decimal? MarginNet { get; internal set; }

    public decimal? MarginMin { get; internal set; }

    public decimal? TradableBalance { get; internal set; }

    public decimal? GrossBalance { get; internal set; }

    public decimal? Buy { get; internal set; }

    public decimal? Sell { get; internal set; }

    public string Symbol { get; internal set; }

    public override string ToString() => $"{this.UserPnL} | {this.UserSwaps} | {this.MarginBalance} | {this.MarginNet} | {this.MarginMin} | {this.TradableBalance} | {this.GrossBalance} | {this.Buy} | {this.Sell} | {this.Symbol}";
}