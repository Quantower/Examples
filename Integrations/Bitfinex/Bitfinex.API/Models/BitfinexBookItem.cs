// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

namespace Bitfinex.API.Models
{
    public class BitfinexBookItem
    {
        public string Pair { get; internal set; }

        public decimal Price { get; internal set; }

        public decimal Amount { get; internal set; }

        public int Count { get; internal set; }

        public override string ToString() => $"{this.Pair} | {this.Price} | {this.Amount} | {this.Count}";
    }
}