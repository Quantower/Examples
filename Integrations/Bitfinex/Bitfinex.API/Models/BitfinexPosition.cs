// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace Bitfinex.API.Models;

public class BitfinexPosition
{
    public string Symbol { get; internal set; }

    public string Status { get; internal set; }

    public double Amount { get; internal set; }

    public double BasePrice { get; internal set; }

    public double FundingAmount { get; internal set; }

    public int FundingType { get; internal set; }

    public double? PnL { get; internal set; }

    public double? PnLPercentage { get; internal set; }

    public double? LiquidationPrice { get; internal set; }

    public double? Leverage { get; internal set; }

    public long Id { get; internal set; }

    public DateTime? CreationTime { get; internal set; }

    public DateTime? UpdateTime { get; internal set; }

    public int Type { get; internal set; }

    public double? Collateral { get; internal set; }

    public double? MinCollateral { get; internal set; }
}