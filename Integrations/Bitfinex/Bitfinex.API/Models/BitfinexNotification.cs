// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;

namespace Bitfinex.API.Models
{
    public class BitfinexNotification
    {
        public DateTime Timestamp { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public string Text { get; set; }
    }
}