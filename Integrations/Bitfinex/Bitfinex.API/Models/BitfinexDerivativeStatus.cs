// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace Bitfinex.API.Models;

public class BitfinexDerivativeStatus
{
    public string Symbol { get; internal set; }

    public DateTime Timestamp { get; internal set; }

    public double? Price { get; internal set; }

    public double? SpotPrice { get; internal set; }

    public double? InsuranceFundBalance { get; internal set; }

    public DateTime NextFundingTimestamp { get; internal set; }

    public double? NextFundingAccrued { get; internal set; }

    public long? NextFundingStep { get; internal set; }

    public double? CurrentFunding { get; internal set; }

    public double? MarkPrice { get; internal set; }

    public double? OpenInterest { get; internal set; }

    public double? ClampMin { get; internal set; }

    public double? ClampMax { get; internal set; }

    public override string ToString() => $"{this.Timestamp} | {this.OpenInterest}";
}