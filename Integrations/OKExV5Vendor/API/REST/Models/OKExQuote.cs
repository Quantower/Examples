// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExQuote : IOKExQuote
{
    public double? AskPrice { get; }
    public double? AskSize { get; }
    public double? BidPrice { get; }
    public double? BidSize { get; }
    public DateTime Time { get; }

    public OKExQuote(OKExOrderBookItem bid, OKExOrderBookItem ask, DateTime time)
    {
        this.AskPrice = ask.Price;
        this.AskSize = ask.Size;

        this.BidPrice = bid.Price;
        this.BidSize = bid.Size;

        this.Time = time;
    }
}