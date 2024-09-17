// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

namespace Bitfinex.API.Models;

public class BitfinexTicker
{
    public string Pair { get; internal set; }

    public decimal Bid { get; internal set; }

    public decimal BidSize { get; internal set; }

    public decimal Ask { get; internal set; }

    public decimal AskSize { get; internal set; }

    public decimal DailyChange { get; internal set; }

    public decimal DailyChangePercent { get; internal set; }

    public decimal LastPrice { get; internal set; }

    public decimal Volume { get; internal set; }

    public decimal High { get; internal set; }

    public decimal Low { get; internal set; }

    public override string ToString() => $"{this.Pair} | {this.Bid}-{this.Ask} | {this.DailyChangePercent:P2} | {this.LastPrice} | {this.Volume}";
}