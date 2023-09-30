// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;

namespace Bitfinex.API.Models;

public class BitfinexOrder
{
    public long Id { get; internal set; }

    public long? GroupId { get; internal set; }

    public long ClientOrderId { get; internal set; }

    public string Symbol { get; internal set; }

    public DateTime CreationTime { get; internal set; }

    public DateTime UpdateTime { get; internal set; }

    public double Amount { get; internal set; }

    public double OriginalAmount { get; internal set; }

    public string Type { get; internal set; }

    public string PreviousType { get; internal set; }

    public DateTime? ExpirationTime { get; internal set; }

    public BitfinexOrderFlags Flags { get; internal set; }

    public string Status { get; internal set; }

    public double Price { get; internal set; }

    public double AveragePrice { get; internal set; }

    public double TrailingPrice { get; internal set; }

    public double AuxiliaryLimitPrice { get; internal set; }

    public bool Hidden { get; internal set; }

    public long PlacedId { get; internal set; }

    public string Routing { get; internal set; }

    public BitfinexMeta Meta { get; internal set; }

    public override string ToString() => $"{this.Symbol} | {this.Amount}/{this.OriginalAmount} | {this.Type}";
}