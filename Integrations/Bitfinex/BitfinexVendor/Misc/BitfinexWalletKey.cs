// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace BitfinexVendor.Misc;

public readonly struct BitfinexWalletKey : IEquatable<BitfinexWalletKey>
{
    private readonly string type;
    private readonly string currency;

    public BitfinexWalletKey(string type, string currency)
    {
        this.type = type;
        this.currency = currency;
    }

    public bool Equals(BitfinexWalletKey other) =>
        this.type == other.type && this.currency == other.currency;

    public override bool Equals(object obj) =>
        obj is BitfinexWalletKey other && this.Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((this.type != null ? this.type.GetHashCode() : 0) * 397) ^ (this.currency != null ? this.currency.GetHashCode() : 0);
        }
    }
}