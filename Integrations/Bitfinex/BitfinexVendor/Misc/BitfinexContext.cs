// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Bitfinex.API.Models;
using BitfinexVendor.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor.Misc;

public class BitfinexContext : IDisposable
{
    #region Properties

    public IDictionary<string, string> AssetLabels { get; }

    public IDictionary<string, BitfinexSymbolDetails> Symbols { get; }

    public HashSet<string> Derivatives { get; }

    public IDictionary<string, BitfinexDerivativeStatus> DerivativeStatusMap { get; }

    public IDictionary<string, BitfinexTicker> Tickers { get; }

    public IDictionary<string, long> LastTradeTimes { get; }

    public CrossRateCache CrossRates { get; }

    public IDictionary<BitfinexWalletKey, BitfinexWallet> Wallets { get; }

    public BitfinexMarginInfo MarginInfo { get; private set; }

    public IDictionary<string, BitfinexMarginInfo> SymbolsMarginInfo { get; }

    public IDictionary<long, BitfinexPosition> Positions { get; }

    public BitfinexUserInfo UserInfo { get; internal set; }

    public BitfinexAccountSummary AccountSummary { get; internal set; }

    private static readonly Regex perpetualAssetRegex;

    #endregion Properties

    static BitfinexContext()
    {
        perpetualAssetRegex = new Regex("([a-zA-Z0-9]+(?<!IX))(IX)?F0");
    }

    public BitfinexContext()
    {
        this.AssetLabels = new Dictionary<string, string>();
        this.Symbols = new Dictionary<string, BitfinexSymbolDetails>();
        this.Derivatives = new HashSet<string>();
        this.DerivativeStatusMap = new Dictionary<string, BitfinexDerivativeStatus>();
        this.Tickers = new Dictionary<string, BitfinexTicker>();
        this.LastTradeTimes = new Dictionary<string, long>();
        this.CrossRates = new CrossRateCache();
        this.Wallets = new Dictionary<BitfinexWalletKey, BitfinexWallet>();
        this.SymbolsMarginInfo = new Dictionary<string, BitfinexMarginInfo>();
        this.Positions = new Dictionary<long, BitfinexPosition>();
    }

    public void Dispose()
    {
        this.AssetLabels.Clear();
        this.Symbols.Clear();
        this.Derivatives.Clear();
        this.Tickers.Clear();
        this.LastTradeTimes.Clear();
        this.CrossRates.Dispose();
    }

    public static bool TryParseAssets(string pair, out string baseAsset, out string quoteAsset)
    {
        baseAsset = default;
        quoteAsset = default;

        if (pair.Length == 6)
        {
            baseAsset = pair.Substring(0, 3);
            quoteAsset = pair.Substring(3, 3);
            return true;
        }

        int separatorIndex = pair.IndexOf(BitfinexVendor.SYMBOL_SEPARATOR, StringComparison.Ordinal);

        if (separatorIndex < 0)
            return false;

        baseAsset = pair.Substring(0, separatorIndex);
        quoteAsset = pair.Substring(separatorIndex + 1);
        return true;
    }

    public static string GetAssetName(string asset)
    {
        var match = perpetualAssetRegex.Match(asset);
        if (!match.Success || match.Groups.Count < 2)
            return asset;

        return match.Groups[1].Value;
    }

    public int GetSymbolPrecision(string pair)
    {
        if (!this.Tickers.TryGetValue(pair, out var ticker))
            return 8;

        return ticker.LastPrice switch
        {
            < 0.1m => 6,
            < 1m => 5,
            < 10m => 4,
            < 100m => 3,
            < 1000m => 2,
            < 10000m => 1,
            _ => 0
        };
    }

    public void UpdateTickers(BitfinexTicker[] tickers)
    {
        foreach (var ticker in tickers)
        {
            this.Tickers[ticker.Pair] = ticker;

            if (!TryParseAssets(ticker.Pair, out string baseAsset, out string quoteAsset))
                continue;

            this.CrossRates.ProcessPrice(baseAsset, quoteAsset, (double)ticker.LastPrice);
        }
    }

    public void UpdateWallets(params BitfinexWallet[] wallets)
    {
        foreach (var wallet in wallets)
            this.Wallets[wallet.GetKey()] = wallet;
    }

    public void UpdateMarginInfo(BitfinexMarginInfo marginInfo) => this.MarginInfo = marginInfo;

    public void UpdateSymbolMarginInfo(params BitfinexMarginInfo[] items)
    {
        foreach (var item in items)
            this.SymbolsMarginInfo[item.Symbol] = item;
    }

    public void UpdatePositions(params BitfinexPosition[] positions)
    {
        foreach (var position in positions)
        {
            if (position.Status == BitfinexPositionStatus.CLOSED)
                this.Positions.Remove(position.Id);
            else
                this.Positions[position.Id] = position;
        }
    }
}