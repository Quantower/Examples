// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Bitfinex.API.Models;
using Bitfinex.API.Models.Requests;
using BitfinexVendor.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor;

internal class BitfinexMarketDataVendor : BitfinexInternalVendor
{
    #region Properties

    private readonly AggressorFlagCalculator aggressorFlagCalculator;

    private Task updateDerivativesStatusTask;

    #endregion Properties

    public BitfinexMarketDataVendor()
    {
        this.aggressorFlagCalculator = new AggressorFlagCalculator();
    }

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters parameters)
    {
        var baseResult = base.Connect(parameters);
        if (baseResult.State != ConnectionState.Connected)
            return baseResult;

        var cancellation = parameters.CancellationToken;

        // Assets
        string[][][] assetLabels = this.HandleApiResponse(
            () => this.Api.PublicRestApiV2.GetConfigs<string[][][]>("map", "currency", "label", cancellation), cancellation, out string error);

        foreach (string[] item in assetLabels[0])
            this.Context.AssetLabels[item[0]] = item[1];

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        // Symbols
        var symbols = this.HandleApiResponse(
            () => this.Api.RestApiV1.GetSymbolDetails(cancellation), cancellation, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        foreach (var symbol in symbols)
            this.Context.Symbols.Add($"t{symbol.Pair}", symbol);

        // Derivatives
        string[][] derivatives = this.HandleApiResponse(
            () => this.Api.PublicRestApiV2.GetConfigs<string[][]>("list", "pair", "futures", cancellation), cancellation, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        foreach (string pair in derivatives[0])
            this.Context.Derivatives.Add(pair);

        // Derivatives status
        var statuses = this.HandleApiResponse(
            () => this.Api.PublicRestApiV2.GetDerivativesStatus(cancellation), cancellation, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        foreach (var status in statuses)
            this.Context.DerivativeStatusMap[status.Symbol] = status;

        // Tickers
        var tickers = this.HandleApiResponse(
            () => this.Api.PublicRestApiV2.GetTickers(cancellation), cancellation, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        this.Context.UpdateTickers(tickers);

        return ConnectionResult.CreateSuccess();
    }

    public override void OnConnected(CancellationToken token)
    {
        this.Api.PublicWebSocketApi.NewData += this.PublicWebSocketApiOnNewData;
        this.Api.PublicWebSocketApi.Error += this.WebSocketApiOnError;

        base.OnConnected(token);
    }

    public override void Disconnect()
    {
        base.Disconnect();

        if (this.Api.PublicWebSocketApi != null)
        {
            this.Api.PublicWebSocketApi.NewData -= this.PublicWebSocketApiOnNewData;
            this.Api.PublicWebSocketApi.Error -= this.WebSocketApiOnError;
        }

        this.Context.Symbols.Clear();
        this.aggressorFlagCalculator.Dispose();
    }

    #endregion Connection

    #region Symbols and symbol groups

    public override IList<MessageSymbol> GetSymbols(CancellationToken token)
    {
        var result = new List<MessageSymbol>();

        foreach (var item in this.Context.Symbols)
        {
            var bitfinexSymbol = item.Value;

            if (!BitfinexContext.TryParseAssets(bitfinexSymbol.Pair, out string baseAsset, out string quoteAsset))
                continue;

            var messageSymbol = this.CreateMessageSymbol(bitfinexSymbol, baseAsset, quoteAsset);
            result.Add(messageSymbol);

            this.Context.LastTradeTimes.Add(bitfinexSymbol.Pair, 0);
        }

        return result;
    }

    public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => new()
    {
        SymbolTypes = new List<SymbolType> { SymbolType.Crypto, SymbolType.Swap }
    };

    public override IList<MessageAsset> GetAssets(CancellationToken token)
    {
        var result = new List<MessageAsset>();

        foreach (var item in this.Context.Symbols)
        {
            var bitfinexSymbol = item.Value;

            if (!BitfinexContext.TryParseAssets(bitfinexSymbol.Pair, out string baseAsset, out string quoteAsset))
            {
                Core.Instance.Loggers.Log($"{BitfinexVendor.VENDOR_NAME}. Can't create assets for symbol {bitfinexSymbol.Pair}");
                continue;
            }

            var message = this.CreateMessageAsset(baseAsset);
            result.Add(message);

            message = this.CreateMessageAsset(quoteAsset);
            result.Add(message);
        }

        return result;
    }

    public override IList<MessageExchange> GetExchanges(CancellationToken token)
    {
        IList<MessageExchange> exchanges = new List<MessageExchange>
        {
            new()
            {
                Id = BitfinexVendor.EXCHANGE_ID.ToString(),
                ExchangeName = "Exchange"
            }
        };

        return exchanges;
    }

    #endregion Symbols and symbol groups

    #region Rules

    public override IList<MessageRule> GetRules(CancellationToken token) => new List<MessageRule>
    {
        new()
        {
            Name = Rule.ALLOW_TRADING,
            Value = false
        }
    };

    #endregion Rules

    #region Subscriptions

    public override void SubscribeSymbol(SubscribeQuotesParameters parameters)
    {
        string channel = parameters.SubscribeType switch
        {
            SubscribeQuoteType.Quote => BitfinexChannel.TICKER,
            SubscribeQuoteType.Level2 => BitfinexChannel.BOOK,
            SubscribeQuoteType.Last => BitfinexChannel.TRADES,
            _ => null
        };

        if (string.IsNullOrEmpty(channel))
            return;

        this.Api.PublicWebSocketApi.Subscribe(channel, parameters.SymbolId, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(error));
    }

    public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters)
    {
        string channel = parameters.SubscribeType switch
        {
            SubscribeQuoteType.Quote => BitfinexChannel.TICKER,
            SubscribeQuoteType.Level2 => BitfinexChannel.BOOK,
            SubscribeQuoteType.Last => BitfinexChannel.TRADES,
            _ => null
        };

        if (string.IsNullOrEmpty(channel))
            return;

        this.Api.PublicWebSocketApi.Unsubscribe(channel, parameters.SymbolId, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(error));
    }

    #endregion Subscriptions

    #region History

    public override HistoryMetadata GetHistoryMetadata(CancellationToken cancellation) => new()
    {
        AllowedHistoryTypes = new[] { HistoryType.Last },
        DownloadingStep_Tick = TimeSpan.FromDays(10),
        AllowedPeriods = new[]
        {
            Period.TICK1,
            Period.MIN1,
            Period.MIN5,
            Period.MIN15,
            Period.MIN30,
            Period.HOUR1,
            Period.HOUR3,
            Period.HOUR6,
            Period.HOUR12,
            Period.DAY1,
            new(BasePeriod.Day, 7),
            new(BasePeriod.Day, 14),
            Period.MONTH1
        }
    };

    public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters)
    {
        var result = new List<IHistoryItem>();

        string symbol = requestParameters.SymbolId;

        long fromUnix = new DateTimeOffset(requestParameters.FromTime).ToUnixTimeMilliseconds();
        long toUnix = new DateTimeOffset(requestParameters.ToTime).ToUnixTimeMilliseconds();
        var cancellation = requestParameters.CancellationToken;

        var itemsStack = new Stack<List<IHistoryItem>>();

        if (requestParameters.Period.BasePeriod == BasePeriod.Tick)
        {
            long currentToUnix = toUnix;

            while (fromUnix < currentToUnix)
            {
                var trades = this.HandleApiResponse(
                    () => this.Api.PublicRestApiV2.GetTrades(symbol, fromUnix, currentToUnix, cancellation), cancellation, out string _, true, true);

                if (trades == null || trades.Length == 0)
                    break;

                var ticks = trades.Select(CreateHistoryItemLast).ToList();

                itemsStack.Push(ticks);

                currentToUnix = trades.Last().Timestamp - 1;
            }
        }
        else
        {
            string timeFrame = GetTimeFrameFromPeriod(requestParameters.Period);
            long millisecondsInRequestPeriod = requestParameters.Period.Ticks / TimeSpan.TicksPerMillisecond;
            long currentToUnix = toUnix;

            while (fromUnix < currentToUnix)
            {
                var candles = this.HandleApiResponse(
                    () => this.Api.PublicRestApiV2.GetCandles(symbol, timeFrame, fromUnix, currentToUnix, cancellation), cancellation, out string _, true, true);

                if (candles == null || candles.Length == 0)
                    break;

                var bars = candles.Select(CreateHistoryItemBar).ToList();

                itemsStack.Push(bars);

                currentToUnix = candles.Last().Timestamp - millisecondsInRequestPeriod;
            }
        }

        string GetTimeFrameFromPeriod(Period period) => period.Ticks switch
        {
            TimeSpan.TicksPerMinute => BitfinexTimeframe.MINUTE_1,
            5 * TimeSpan.TicksPerMinute => BitfinexTimeframe.MINUTE_5,
            15 * TimeSpan.TicksPerMinute => BitfinexTimeframe.MINUTE_15,
            30 * TimeSpan.TicksPerMinute => BitfinexTimeframe.MINUTE_30,
            TimeSpan.TicksPerHour => BitfinexTimeframe.HOUR_1,
            3 * TimeSpan.TicksPerHour => BitfinexTimeframe.HOUR_3,
            6 * TimeSpan.TicksPerHour => BitfinexTimeframe.HOUR_6,
            12 * TimeSpan.TicksPerHour => BitfinexTimeframe.HOUR_12,
            TimeSpan.TicksPerDay => BitfinexTimeframe.DAY_1,
            7 * TimeSpan.TicksPerDay => BitfinexTimeframe.DAY_7,
            14 * TimeSpan.TicksPerDay => BitfinexTimeframe.DAY_14,
            _ => BitfinexTimeframe.MONTH_1
        };

        while (itemsStack.Count > 0)
        {
            var items = itemsStack.Pop();

            for (int i = items.Count - 1; i >= 0; i--)
                result.Add(items[i]);
        }

        if (result.Any() && requestParameters.Symbol.SymbolType != SymbolType.Crypto)
            this.LoadOpenInterest(symbol, fromUnix, toUnix, result, cancellation);

        return result;
    }

    private void LoadOpenInterest(string symbol, long fromUnix, long toUnix, IList<IHistoryItem> result, CancellationToken cancellation)
    {
        var statuses = this.LoadDerivativeStatus(symbol, fromUnix, toUnix, cancellation);

        int itemIndex = result.Count - 1;
        IHistoryItem lastProcessedItem = null;
        foreach (var status in statuses)
        {
            if (status.OpenInterest == null)
                continue;

            if (lastProcessedItem != null && status.Timestamp >= lastProcessedItem.TimeLeft)
                continue;

            if (itemIndex < 0)
                break;

            var historyItem = result[itemIndex--];

            if (status.Timestamp >= historyItem.TimeLeft)
            {
                SetOpenInterest(historyItem, status.OpenInterest.Value);
                lastProcessedItem = historyItem;
            }
        }

        static void SetOpenInterest(IHistoryItem historyItem, double openInterest)
        {
            switch (historyItem)
            {
                case HistoryItemLast last:
                    last.OpenInterest = openInterest;
                    break;
                case HistoryItemBar bar:
                    bar.OpenInterest = openInterest;
                    break;
            }
        }
    }

    private IEnumerable<BitfinexDerivativeStatus> LoadDerivativeStatus(string symbol, long fromUnix, long toUnix, CancellationToken token)
    {
        const int LIMIT = 5000;

        BitfinexDerivativeStatus[] statuses;

        do
        {
            var request = new BitfinexDerivativeStatusHistoryRequest
            {
                Start = fromUnix,
                End = toUnix,
                Limit = LIMIT
            };

            statuses = this.HandleApiResponse(() => this.Api.PublicRestApiV2.GetDerivativesStatusHistory(symbol, request, token), token, out _, true, true);
            if (statuses == null)
                yield break;

            foreach (var status in statuses)
                yield return status;

            var lastStatus = statuses.LastOrDefault();
            if (lastStatus == null)
                break;

            toUnix = new DateTimeOffset(lastStatus.Timestamp.AddMilliseconds(-1)).ToUnixTimeMilliseconds();
        }
        while (statuses.Length == LIMIT && fromUnix < toUnix);
    }

    #endregion History

    #region Factory

    private MessageAsset CreateMessageAsset(string assetId)
    {
        return new MessageAsset
        {
            Id = assetId,
            Name = assetId,
            MinimumChange = assetId == BitfinexVendor.USER_ASSET_ID ? 1e-2 : 1e-8
        };
    }

    private MessageSymbol CreateMessageSymbol(BitfinexSymbolDetails bitfinexSymbol, string baseAssetId, string quoteAssetId)
    {
        string baseLabel = baseAssetId;
        string quoteLabel = quoteAssetId;

        if (this.Context.AssetLabels.TryGetValue(baseAssetId, out string label))
            baseLabel = label;

        if (this.Context.AssetLabels.TryGetValue(quoteAssetId, out label))
            quoteLabel = label;

        bool isPerpetual = this.Context.Derivatives.Contains(bitfinexSymbol.Pair);

        string baseAssetName = BitfinexContext.GetAssetName(baseAssetId);
        string quoteAssetName = BitfinexContext.GetAssetName(quoteAssetId);

        var message = new MessageSymbol($"t{bitfinexSymbol.Pair}")
        {
            AllowCalculateRealtimeChange = false,
            AllowCalculateRealtimeVolume = false,
            AllowCalculateRealtimeTrades = false,
            AllowCalculateRealtimeTicks = false,
            AllowAbbreviatePriceByTickSize = true,
            Description = GetSymbolDescription(),
            ExchangeId = BitfinexVendor.EXCHANGE_ID.ToString(),
            HistoryType = HistoryType.Last,
            LotSize = 1,
            LotStep = 1e-8,
            NotionalValueStep = 1e-8,
            MinLot = bitfinexSymbol.MinimumOrderSize,
            MaxLot = bitfinexSymbol.MaximumOrderSize,
            Name = GetSymbolName(),
            ProductAssetId = isPerpetual ? null : baseAssetId,
            QuotingCurrencyAssetID = quoteAssetId,
            SymbolType = isPerpetual ? SymbolType.Swap : SymbolType.Crypto,
            VolumeType = SymbolVolumeType.Volume,
            QuotingType = SymbolQuotingType.LotSize,
            DeltaCalculationType = DeltaCalculationType.TickDirection,
            VariableTickList = new List<VariableTick>
            {
                new(Math.Pow(10, -this.Context.GetSymbolPrecision(bitfinexSymbol.Pair)))
            },
            SymbolAdditionalInfo = new List<AdditionalInfoItem>
            {
                new()
                {
                    GroupInfo = BitfinexVendor.TRADING_INFO_GROUP,
                    SortIndex = 100,
                    Id = BitfinexVendor.ALLOW_MARGIN,
                    NameKey = loc.key("Allow margin trading"),
                    ToolTipKey = loc.key("Allow margin trading"),
                    DataType = ComparingType.String,
                    Value = bitfinexSymbol.AllowMargin,
                    Hidden = false
                },
            }
        };

        if (bitfinexSymbol.AllowMargin)
        {
            message.SymbolAdditionalInfo.Add(new()
            {
                GroupInfo = BitfinexVendor.TRADING_INFO_GROUP,
                SortIndex = 110,
                Id = "Initial margin",
                NameKey = loc.key("Initial margin"),
                ToolTipKey = loc.key("Initial margin"),
                DataType = ComparingType.Double,
                Value = bitfinexSymbol.InitialMargin,
                Hidden = false
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                GroupInfo = BitfinexVendor.TRADING_INFO_GROUP,
                SortIndex = 120,
                Id = "Minimum margin",
                NameKey = loc.key("Minimum margin"),
                ToolTipKey = loc.key("Minimum margin"),
                DataType = ComparingType.Double,
                Value = bitfinexSymbol.MinimumMargin,
                Hidden = false
            });
        }

        if (this.Context.DerivativeStatusMap.TryGetValue(message.Id, out var derivativeStatus))
        {
            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "insuranceFundBalance",
                NameKey = loc.key("Insurance fund balance"),
                ToolTipKey = loc.key("The balance available to the liquidation engine to absorb losses"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 10,
                DataType = ComparingType.Double,
                Value = derivativeStatus.InsuranceFundBalance
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "nextFundingTimestamp",
                NameKey = loc.key("Next funding"),
                ToolTipKey = loc.key("Timestamp of next funding event"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 20,
                DataType = ComparingType.DateTime,
                Value = derivativeStatus.NextFundingTimestamp,
                FormattingDescription = new DateTimeFormattingDescription(derivativeStatus.NextFundingTimestamp)
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "nextFundingAccrued",
                NameKey = loc.key("Next funding accrued"),
                ToolTipKey = loc.key("Current accrued funding for next 8h period"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 30,
                DataType = ComparingType.Double,
                Value = derivativeStatus.NextFundingAccrued
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "nextFundingStep",
                NameKey = loc.key("Next funding step"),
                ToolTipKey = loc.key("Incremental accrual counter"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 40,
                DataType = ComparingType.Long,
                Value = derivativeStatus.NextFundingStep
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "currentFunding",
                NameKey = loc.key("Current funding"),
                ToolTipKey = loc.key("Funding applied in the current 8h period"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 50,
                DataType = ComparingType.Double,
                Value = derivativeStatus.CurrentFunding
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "clampMin",
                NameKey = loc.key("Clamp min"),
                ToolTipKey = loc.key("Range in the average spread that does not require a funding payment"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 60,
                DataType = ComparingType.Double,
                Value = derivativeStatus.ClampMin
            });

            message.SymbolAdditionalInfo.Add(new()
            {
                Id = "clampMax",
                NameKey = loc.key("Clamp max"),
                ToolTipKey = loc.key("Funding payment cap"),
                GroupInfo = BitfinexVendor.FUNDING_GROUP,
                SortIndex = 70,
                DataType = ComparingType.Double,
                Value = derivativeStatus.ClampMax
            });
        }

        string GetSymbolName()
        {
            if (isPerpetual)
            {
                return quoteAssetName.StartsWith("UST") ?
                    $"{baseAssetName}-PERP" :
                    $"{baseAssetName}/{quoteAssetName}-PERP";
            }

            return $"{baseAssetName}/{quoteAssetName}";
        }

        string GetSymbolDescription()
        {
            if (isPerpetual)
            {
                return quoteAssetName.StartsWith("UST") ?
                    $"{baseAssetName} Perpetual derivative" :
                    $"{baseAssetName}/{quoteAssetName} Perpetual derivative";
            }

            return $"{baseLabel} vs {quoteLabel}";
        }

        return message;
    }

    private DayBar CreateDayBar(BitfinexTicker bitfinexTicker) => new(bitfinexTicker.Pair, Core.Instance.TimeUtils.DateTimeUtcNow)
    {
        Change = (double)bitfinexTicker.DailyChange,
        ChangePercentage = (double)(bitfinexTicker.DailyChangePercent * 100),
        High = (double)bitfinexTicker.High,
        Low = (double)bitfinexTicker.Low,
        Volume = (double)bitfinexTicker.Volume,
        OpenInterest = this.GetOpenInterest(bitfinexTicker.Pair)
    };

    private DayBar CreateDayBar(BitfinexTrade bitfinexTrade) => new(bitfinexTrade.Pair, DateTimeOffset.FromUnixTimeSeconds((int)bitfinexTrade.Timestamp).UtcDateTime)
    {
        Last = (double)bitfinexTrade.Price,
        LastSize = (double)Math.Abs(bitfinexTrade.Amount),
        OpenInterest = this.GetOpenInterest(bitfinexTrade.Pair)
    };

    private DayBar CreateDayBar(BitfinexDerivativeStatus derivativeStatus) => new(derivativeStatus.Symbol, Core.Instance.TimeUtils.DateTimeUtcNow)
    {
        OpenInterest = derivativeStatus.OpenInterest ?? Const.DOUBLE_UNDEFINED,
        Mark = derivativeStatus.MarkPrice ?? Const.DOUBLE_UNDEFINED
    };

    private Quote CreateQuote(BitfinexTicker bitfinexTicker)
    {
        var dateTime = Core.Instance.TimeUtils.DateTimeUtcNow;

        if (this.Context.LastTradeTimes.TryGetValue(bitfinexTicker.Pair, out long lastTradeTime) && dateTime.Ticks <= lastTradeTime)
            dateTime = new DateTime(lastTradeTime + 1, DateTimeKind.Utc);

        this.Context.LastTradeTimes[bitfinexTicker.Pair] = dateTime.Ticks;

        return new Quote(bitfinexTicker.Pair, (double)bitfinexTicker.Bid, (double)bitfinexTicker.BidSize, (double)bitfinexTicker.Ask, (double)bitfinexTicker.AskSize, dateTime);
    }

    private static DOMQuote CreateDomQuote(IReadOnlyCollection<BitfinexBookItem> bitfinexBook)
    {
        var utcNow = Core.Instance.TimeUtils.DateTimeUtcNow;
        string symbol = bitfinexBook.First().Pair;

        var bids = bitfinexBook.Where(b => b.Amount > 0).OrderBy(b => b.Price);
        var asks = bitfinexBook.Where(b => b.Amount < 0).OrderBy(b => b.Price);

        var dom = new DOMQuote(symbol, utcNow);

        foreach (var bid in bids)
        {
            string id = $"Id_{bid.Price}";
            double price = (double)bid.Price;
            double size = bid.Count == 0 ? 0 : Math.Abs((double)bid.Amount);

            dom.Bids.Add(new Level2Quote(QuotePriceType.Bid, symbol, id, price, size, utcNow));
        }

        foreach (var ask in asks)
        {
            string id = $"Id_{ask.Price}";
            double price = (double)ask.Price;
            double size = ask.Count == 0 ? 0 : Math.Abs((double)ask.Amount);

            dom.Asks.Add(new Level2Quote(QuotePriceType.Ask, symbol, id, price, size, utcNow));
        }

        return dom;
    }

    private static Level2Quote CreateLevel2Quote(BitfinexBookItem bitfinexBookItem)
    {
        var priceType = bitfinexBookItem.Amount > 0 ? QuotePriceType.Bid : QuotePriceType.Ask;
        string symbol = bitfinexBookItem.Pair;
        string id = $"Id_{bitfinexBookItem.Price}";
        double price = (double)bitfinexBookItem.Price;
        double size = bitfinexBookItem.Count == 0 ? 0 : Math.Abs((double)bitfinexBookItem.Amount);
        var utcNow = Core.Instance.TimeUtils.DateTimeUtcNow;

        return new Level2Quote(priceType, symbol, id, price, size, utcNow);
    }

    private Last CreateLast(BitfinexTrade bitfinexTrade)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds((int)bitfinexTrade.Timestamp).UtcDateTime;

        if (this.Context.LastTradeTimes.TryGetValue(bitfinexTrade.Pair, out long lastTradeTime) && dateTime.Ticks <= lastTradeTime)
            dateTime = new DateTime(lastTradeTime + 1, DateTimeKind.Utc);

        this.Context.LastTradeTimes[bitfinexTrade.Pair] = dateTime.Ticks;

        return new Last(bitfinexTrade.Pair, (double)bitfinexTrade.Price, Math.Abs((double)bitfinexTrade.Amount), dateTime)
        {
            TradeId = bitfinexTrade.Id.ToString(),
            OpenInterest = this.GetOpenInterest(bitfinexTrade.Pair)
        };
    }

    private static IHistoryItem CreateHistoryItemLast(BitfinexTrade bitfinexTrade) => new HistoryItemLast
    {
        TicksLeft = DateTimeOffset.FromUnixTimeMilliseconds(bitfinexTrade.Timestamp).UtcDateTime.Ticks,
        Price = (double)bitfinexTrade.Price,
        Volume = Math.Abs((double)bitfinexTrade.Amount)
    };

    private static IHistoryItem CreateHistoryItemBar(BitfinexCandle bitfinexCandle) => new HistoryItemBar
    {
        TicksLeft = DateTimeOffset.FromUnixTimeMilliseconds(bitfinexCandle.Timestamp).UtcDateTime.Ticks,
        Open = (double)bitfinexCandle.Open,
        High = (double)bitfinexCandle.High,
        Low = (double)bitfinexCandle.Low,
        Close = (double)bitfinexCandle.Close,
        Volume = (double)bitfinexCandle.Volume
    };

    #endregion Factory

    #region Periodic actions

    private void UpdateDerivativesStatusAction()
    {
        var statuses = this.HandleApiResponse(
            () => this.Api.PublicRestApiV2.GetDerivativesStatus(this.GlobalCancellation), this.GlobalCancellation, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateDerivativesStatusAction)}: {error}", LoggingLevel.Error, BitfinexVendor.VENDOR_NAME);
            return;
        }

        foreach (var status in statuses)
        {
            try
            {
                this.Context.DerivativeStatusMap[status.Symbol] = status;

                if (!this.Context.Symbols.TryGetValue(status.Symbol, out var symbolDetails))
                    continue;

                if (BitfinexContext.TryParseAssets(symbolDetails.Pair, out string baseAsset, out string quoteAsset))
                    this.PushMessage(this.CreateMessageSymbol(symbolDetails, baseAsset, quoteAsset));

                this.PushMessage(this.CreateDayBar(status));
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex, nameof(this.UpdateDerivativesStatusAction));
            }
        }
    }

    #endregion Periodic actions

    #region Misc

    private double GetOpenInterest(string symbol)
    {
        double openInterest = Const.DOUBLE_UNDEFINED;
        if (this.Context.DerivativeStatusMap.TryGetValue(symbol, out var derivativeStatus) && derivativeStatus.OpenInterest != null)
            openInterest = derivativeStatus.OpenInterest.Value;

        return openInterest;
    }

    #endregion Misc

    private void PublicWebSocketApiOnNewData(object sender, BitfinexEventArgs e)
    {
        if (e.Ticker != null)
        {
            var dayBar = this.CreateDayBar(e.Ticker);
            this.PushMessage(dayBar);

            var quote = this.CreateQuote(e.Ticker);

            this.aggressorFlagCalculator.CollectBidAsk(quote);

            this.PushMessage(quote);
        }

        if (e.Book != null)
        {
            var dom = CreateDomQuote(e.Book);
            this.PushMessage(dom);
        }

        if (e.BookItem != null)
        {
            var level2 = CreateLevel2Quote(e.BookItem);
            this.PushMessage(level2);
        }

        if (e.Trade != null)
        {
            if (e.IsSnapshotData)
            {
                var dayBar = this.CreateDayBar(e.Trade);
                this.PushMessage(dayBar);
            }
            else
            {
                var last = this.CreateLast(e.Trade);

                last.AggressorFlag = this.aggressorFlagCalculator.CalculateAggressorFlag(last);

                this.PushMessage(last);
            }
        }
    }

    private protected void WebSocketApiOnError(object sender, BitfinexErrorEventArgs e)
    {
        if (e.Exception == null)
            return;

        this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(e.Exception.GetFullMessageRecursive()));
    }

    private protected override void OnTimerTick()
    {
        base.OnTimerTick();

        this.updateDerivativesStatusTask ??= Task.Run(this.UpdateDerivativesStatusAction)
            .ContinueWith(t => this.updateDerivativesStatusTask = null);
    }
}