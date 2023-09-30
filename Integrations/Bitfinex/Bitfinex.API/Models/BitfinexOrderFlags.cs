// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;

namespace Bitfinex.API.Models;

[Flags]
public enum BitfinexOrderFlags
{
    Hidden = 64,
    Close = 512,
    ReduceOnly = 1024,
    PostOnly = 4096,
    Oco = 16384,
    NoVarRates = 524288
}