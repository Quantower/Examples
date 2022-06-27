// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

namespace Bitfinex.API.Models
{
    public class BitfinexTrade
    {
        public string Pair { get; internal set; }

        public long Id { get; internal set; }

        public long Timestamp { get; internal set; }

        public decimal Price { get; internal set; }

        public decimal Amount { get; internal set; }

        public override string ToString() => $"{this.Id} | {this.Timestamp} | {this.Price} | {this.Amount}";
    }
}
