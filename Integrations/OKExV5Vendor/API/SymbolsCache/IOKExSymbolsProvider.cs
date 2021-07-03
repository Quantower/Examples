using OKExV5Vendor.API;
using OKExV5Vendor.API.REST.Models;
using System.Collections.Generic;

namespace OKExV5Vendor.API.SymbolsCache
{
    interface IOKExSymbolsProvider
    {
        IEnumerable<OKExSymbol> GetSymbols(params OKExInstrumentType[] types);
        bool TryGetFuturesByUnderlier(string underlierId, out IList<OKExSymbol> futures);
        bool TryGetOptionsByUnderlier(string underlierId, out IList<OKExSymbol> options);
        bool TryGetSymbolById(string symbolId, OKExInstrumentType type, out OKExSymbol symbol);
    }
}
