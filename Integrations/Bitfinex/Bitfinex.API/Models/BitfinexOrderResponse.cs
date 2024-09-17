// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace Bitfinex.API.Models;

public class BitfinexOrderResponse
{
    public DateTime Timestamp { get; internal set; }

    public string Type { get; internal set; }

    public BitfinexOrder[] Orders { get; internal set; }

    public string Status { get; internal set; }

    public string Text { get; internal set; }
}