// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

namespace Bitfinex.API.Models;

public class BitfinexCandle
{
    public long Timestamp { get; internal set; }

    public decimal Open { get; internal set; }

    public decimal Close { get; internal set; }

    public decimal High { get; internal set; }

    public decimal Low { get; internal set; }

    public decimal Volume { get; internal set; }

    public override string ToString() => $"{this.Timestamp} | O: {this.Open} | H: {this.High} | L: {this.Low} | C: {this.Close} | V: {this.Volume}";
}