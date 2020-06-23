// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

namespace BitfinexVendor.API.Models
{
    class BitfinexBookItem
    {
        public string Pair { get; internal set; }

        public decimal Price { get; internal set; }

        public decimal Amount { get; internal set; }

        public int Count { get; internal set; }
    }
}
