using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket.Models;
using System.Collections.Generic;

namespace OKExV5Vendor.API.Misc
{
    interface IOKExOrderEntryDataProvider
    {
        IReadOnlyDictionary<string, OKExBalanceItem> Balances { get; }
        OKExBalance TotalInfo { get; }
        OKExAccount Account { get; }

        OKExSymbol GetSymbol(string id);
        void PopulateLeverage(OKExSymbol symbol);
    }
}
