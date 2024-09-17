// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace OKExV5Vendor.API.REST.Models;

internal interface IOKExIndexPrice
{
    public double? IndexPrice { get; }

    public DateTime Time { get; }
}