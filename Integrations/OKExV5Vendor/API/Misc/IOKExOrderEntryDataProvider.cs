// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket.Models;
using System.Collections.Generic;

namespace OKExV5Vendor.API.Misc;

internal interface IOKExOrderEntryDataProvider
{
    IReadOnlyDictionary<string, OKExBalanceItem> Balances { get; }
    OKExBalance TotalInfo { get; }
    OKExAccount Account { get; }

    OKExSymbol GetSymbol(string id);
}