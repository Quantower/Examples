// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;

namespace Bitfinex.API.Models
{
    public class BitfinexUserTrade
    {
        public long Id { get; internal set; }

        public string Pair { get; internal set; }

        public DateTime ExecutionTime { get; internal set; }

        public long OrderId { get; internal set; }

        public double Amount { get; internal set; }

        public double Price { get; internal set; }

        public string OrderType { get; internal set; }

        public double OrderPrice { get; internal set; }

        public bool IsMaker { get; internal set; }

        public double? Fee { get; internal set; }

        public string FeeCurrency { get; internal set; }
    }
}