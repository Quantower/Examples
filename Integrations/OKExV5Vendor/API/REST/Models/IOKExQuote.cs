// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace OKExV5Vendor.API.REST.Models;

internal interface IOKExQuote
{
    public double? AskPrice { get; }
    public double? AskSize { get; }
    public double? BidPrice { get; }
    public double? BidSize { get; }

    public DateTime Time { get; }
}