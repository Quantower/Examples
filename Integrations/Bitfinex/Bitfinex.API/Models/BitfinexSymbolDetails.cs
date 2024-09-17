// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Runtime.Serialization;

namespace Bitfinex.API.Models;

[DataContract]
public class BitfinexSymbolDetails
{
    [DataMember(Name = "pair")]
    public string Pair
    {
        get => this.pair;
        private set => this.pair = value?.ToUpper();
    }
    private string pair;

    [DataMember(Name = "price_precision")]
    public int PricePrecision { get; set; }

    [DataMember(Name = "initial_margin")]
    public double InitialMargin { get; set; }

    [DataMember(Name = "minimum_margin")]
    public double MinimumMargin { get; set; }

    [DataMember(Name = "maximum_order_size")]
    public double MaximumOrderSize { get; set; }

    [DataMember(Name = "minimum_order_size")]
    public double MinimumOrderSize { get; set; }

    [DataMember(Name = "expiration")]
    public string Expiration { get; set; }

    [DataMember(Name = "margin")]
    public bool AllowMargin { get; set; }

    public override string ToString() => this.Pair;
}