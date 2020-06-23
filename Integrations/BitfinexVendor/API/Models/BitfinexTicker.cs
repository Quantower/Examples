// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

namespace BitfinexVendor.API.Models
{
    class BitfinexTicker
    {
        public string Pair { get; internal set; }

        public decimal Bid { get; internal set; }

        public decimal BidSize { get; internal set; }

        public decimal Ask { get; internal set; }

        public decimal AskSize { get; internal set; }

        public decimal DailyChange { get; internal set; }

        public decimal DailyChangePercent { get; internal set; }

        public decimal LastPrice { get; internal set; }

        public decimal Volume { get; internal set; }

        public decimal High { get; internal set; }

        public decimal Low { get; internal set; }
    }
}
