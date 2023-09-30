// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using OKExV5Vendor.API.REST.Models;
using System.Collections.Generic;
using System.Linq;

namespace OKExV5Vendor.API.SymbolsCache;

class OKExSymbolCache : IOKExSymbolsProvider
{
    #region Parameters

    private readonly IDictionary<OKExInstrumentType, IDictionary<string, OKExSymbol>> allSymbolsCache;
    private IDictionary<string, List<OKExSymbol>> optionsCacheByUnderlier;
    private IDictionary<string, List<OKExSymbol>> futuresCacheByUnderlier;

    internal int AvailableTypesCount => this.populatedTypesCounter;
    private int populatedTypesCounter;

    #endregion Parameters

    public OKExSymbolCache()
    {
        this.allSymbolsCache = new Dictionary<OKExInstrumentType, IDictionary<string, OKExSymbol>>();
        this.populatedTypesCounter = 0;
        this.optionsCacheByUnderlier = new Dictionary<string, List<OKExSymbol>>();
        this.futuresCacheByUnderlier = new Dictionary<string, List<OKExSymbol>>();
    }

    internal bool Contains(OKExInstrumentType type) => this.allSymbolsCache.ContainsKey(type);
    internal void AddSymbols(OKExInstrumentType type, IEnumerable<OKExSymbol> symbols)
    {
        if (!this.allSymbolsCache.TryGetValue(type, out var cache))
            this.allSymbolsCache[type] = cache = new Dictionary<string, OKExSymbol>();

        foreach (var s in symbols)
            cache[s.OKExInstrumentId] = s;

        switch (type)
        {
            case OKExInstrumentType.Futures:
                {
                    this.PopulateFuturesByUnderlierCache();
                    break;
                }
            case OKExInstrumentType.Option:
                {
                    this.PopulateOptionsByUnderlierCache();
                    break;
                }
        }

        this.populatedTypesCounter++;
    }
    internal void UpdateSymbols(OKExSymbol[] symbols)
    {
        bool needRepopulateFutures = false;
        bool needRepopulateOptions = false;

        foreach (var s in symbols)
        {
            if (this.allSymbolsCache.TryGetValue(s.InstrumentType, out var symbolsCache))
            {
                symbolsCache[s.OKExInstrumentId] = s;

                if (!needRepopulateFutures)
                    needRepopulateFutures = s.InstrumentType == OKExInstrumentType.Futures;

                if (!needRepopulateOptions)
                    needRepopulateOptions = s.InstrumentType == OKExInstrumentType.Option;
            }
        }

        if (needRepopulateFutures)
            this.PopulateFuturesByUnderlierCache();

        if (needRepopulateOptions)
            this.PopulateOptionsByUnderlierCache();
    }
    internal void PopulateIndexes()
    {
        var underliers = this.allSymbolsCache.Values
            .SelectMany(s => s.Values)
            .Where(s => s.HasUnderlier)
            .Select(s => s.Underlier)
            .Distinct();

        this.allSymbolsCache[OKExInstrumentType.Index] = new Dictionary<string, OKExSymbol>();
        foreach (var indexId in underliers)
        {
            if (this.CreateIndex(indexId) is OKExSymbol indexSymbol)
                this.allSymbolsCache[OKExInstrumentType.Index][indexSymbol.OKExInstrumentId] = indexSymbol;
        }
    }

    #region IOKExSymbolsProvider

    public IEnumerable<OKExSymbol> GetSymbols(params OKExInstrumentType[] types)
    {
        var result = new List<OKExSymbol>();

        foreach (var item in types)
        {
            if (this.allSymbolsCache.TryGetValue(item, out var symbols))
                result.AddRange(symbols.Values);
        }

        return result;
    }
    public bool TryGetFuturesByUnderlier(string underlierId, out IList<OKExSymbol> futures)
    {
        if (this.futuresCacheByUnderlier.TryGetValue(underlierId, out var symbols))
        {
            futures = symbols.ToList();
            return true;
        }
        else
        {
            futures = null;
            return false;
        }
    }
    public bool TryGetOptionsByUnderlier(string underlierId, out IList<OKExSymbol> strikes)
    {
        if (this.optionsCacheByUnderlier.TryGetValue(underlierId, out var symbols))
        {
            strikes = symbols.ToList();
            return true;
        }
        else
        {
            strikes = null;
            return false;
        }
    }
    public bool TryGetSymbolById(string symbolId, OKExInstrumentType type, out OKExSymbol symbol)
    {
        symbol = null;

        foreach (var item in this.allSymbolsCache)
        {
            if (type != OKExInstrumentType.Any && type == item.Key)
                return item.Value.TryGetValue(symbolId, out symbol);
        }

        return false;
    }

    #endregion IOKExSymbolsProvider

    #region Misc

    private void PopulateFuturesByUnderlierCache()
    {
        this.futuresCacheByUnderlier.Clear();
        this.futuresCacheByUnderlier = this.allSymbolsCache[OKExInstrumentType.Futures].Values
            .GroupBy(k => k.Underlier)
            .ToDictionary(k => k.Key, v => v.ToList());
    }
    public void PopulateOptionsByUnderlierCache()
    {
        this.optionsCacheByUnderlier.Clear();
        this.optionsCacheByUnderlier = this.allSymbolsCache[OKExInstrumentType.Option].Values
            .GroupBy(k => k.Underlier)
            .ToDictionary(k => k.Key, v => v.ToList());
    }
    private OKExSymbol CreateIndex(string underlierId)
    {
        var splitted = underlierId.Split('-');

        if (splitted.Length < 2)
            return null;

        var baseCurr = splitted[0];
        var quoteCurr = splitted[1];

        var templateSymbol = this.allSymbolsCache.Values.SelectMany(s => s.Values).FirstOrDefault(s => s.BaseCurrency == baseCurr);

        return new OKExSymbol()
        {
            OKExInstrumentId = underlierId,
            SettlementCurrency = baseCurr,
            ContractValueCurrency = quoteCurr,
            TickSize = templateSymbol != null ? templateSymbol.TickSize.Value : 1E8,
            InstrumentType = OKExInstrumentType.Index,
            Name = underlierId,
            Status = OKExInstrumentStatus.Live
        };
    }

    #endregion Misc
}