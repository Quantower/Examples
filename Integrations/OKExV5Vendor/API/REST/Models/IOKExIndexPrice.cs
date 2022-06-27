// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    interface IOKExIndexPrice
    {
        public double? IndexPrice { get; }

        public DateTime Time { get; }
    }
}
