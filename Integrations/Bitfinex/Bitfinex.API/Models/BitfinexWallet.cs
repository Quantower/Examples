// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

namespace Bitfinex.API.Models;

public class BitfinexWallet
{
    public string Type { get; internal set; }

    public string Currency { get; internal set; }

    public decimal Balance { get; internal set; }

    public decimal? UnsettledInterest { get; internal set; }

    public decimal? AvailableBalance { get; internal set; }

    public override string ToString() => $"{this.Type} | {this.Currency} | {this.AvailableBalance}/{this.Balance} | {this.UnsettledInterest}";
}