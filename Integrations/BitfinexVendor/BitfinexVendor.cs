// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using BitfinexVendor.API;
using BitfinexVendor.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor
{
    /// <summary>
    /// An example of integration with for Quantower trading platform
    /// Bitfinex crypto exchange
    /// </summary>
    public class BitfinexVendor : Vendor
    {
        #region Properties
        public event Action<Message> NewMessage;

        private readonly BitfinexRestApi restApi;
        private readonly BitfinexSocketApi socketApi;

        private readonly Dictionary<string, BitfinexSymbol> symbolsCache;

        private readonly Ping ping;
        private readonly Uri pingUri;

        private readonly Dictionary<string, long> lastTradeTimeCache;

        private AggressorFlagCalculator aggressorFlagCalculator;
        #endregion Properties

        public BitfinexVendor()
        {
            this.restApi = new BitfinexRestApi(BitfinexConsts.API_BASE_ENDPOINT);
            this.socketApi = new BitfinexSocketApi();
            this.socketApi.NewData += this.SocketApi_NewData;

            this.symbolsCache = new Dictionary<string, BitfinexSymbol>();
            this.lastTradeTimeCache = new Dictionary<string, long>();

            this.aggressorFlagCalculator = new AggressorFlagCalculator();

            this.ping = new Ping();
            this.pingUri = new Uri(BitfinexConsts.API_BASE_ENDPOINT);
        }

        #region Integration details

        /// <summary>
        /// Use GetVendorMetaData method to provide general information about integration such s name, description, registration link, etc
        /// </summary>        
        public override VendorMetaData GetVendorMetaData() => new VendorMetaData
        {
            VendorName = BitfinexConsts.VENDOR_NAME,
            VendorDescription = loc.key("Market data connection. Trading coming soon.")
        };

        #endregion Integration detailss

        #region Connection

        public override IList<ConnectionInfo> GetDefaultConnections() => new List<ConnectionInfo>
        {
            this.CreateDefaultConnectionInfo("Bitfinex", BitfinexConsts.VENDOR_NAME, "BitfinexVendor\\bitfinex.svg", allowCreateCustomConnections: false)
        };

        /// <summary>
        /// Called when user decides to connect to this particular integration via Connections Screen
        /// </summary>        
        public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return ConnectionResult.CreateFail(loc._("Network does not available"));

            this.socketApi.Connect();

            if (this.socketApi.ConnectionState != BitfinexConnectionState.Connected)
                return ConnectionResult.CreateFail(loc._("Can't connect via socket"));

            var symbols = this.restApi.GetSymbolsDetails().Result;
            foreach (var symbol in symbols)
                this.symbolsCache.Add(symbol.Pair, symbol);

            return ConnectionResult.CreateSuccess();
        }

        /// <summary>
        /// Called when user disconnects from integration
        /// </summary>
        public override void Disconnect()
        {
            this.socketApi.Disconnect();

            this.symbolsCache.Clear();

            this.aggressorFlagCalculator.Dispose();
        }

        /// <summary>
        /// Method Ping called periodically by trading platform to analyse current speed of communication with server
        /// </summary>        
        public override PingResult Ping()
        {
            var result = new PingResult
            {
                State = PingEnum.Disconnected
            };

            if (this.socketApi.ConnectionState != BitfinexConnectionState.Connected)
                return result;

            try
            {
                var pingResult = this.ping.Send(this.pingUri.Host);

                if (pingResult != null && pingResult.Status == IPStatus.Success)
                {
                    result.PingTime = TimeSpan.FromMilliseconds(pingResult.RoundtripTime);
                    result.State = PingEnum.Connected;
                }
                else
                    Core.Instance.Loggers.Log($"{BitfinexConsts.VENDOR_NAME}. Error while pinging host: {this.pingUri.Host}");
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }

            return result;
        }

        #endregion Connection

        #region Symbols and symbol groups       

        /// <summary>
        /// Provides a information about available Symbols into the trading platform
        /// </summary>        
        public override IList<MessageSymbol> GetSymbols(CancellationToken token)
        {
            List<MessageSymbol> result = new List<MessageSymbol>();

            foreach (var item in this.symbolsCache)
            {
                var bitfinexSymbol = item.Value;

                if (bitfinexSymbol.Pair.Length != 6 && bitfinexSymbol.Pair.IndexOf(BitfinexConsts.SYMBOL_SEPARATOR, StringComparison.Ordinal) < 0)
                    continue;

                var messageSymbol = CreateMessageSymbol(bitfinexSymbol);
                result.Add(messageSymbol);

                this.lastTradeTimeCache.Add(bitfinexSymbol.Pair, 0);
            }

            return result;
        }

        /// <summary>
        /// Provides a information about available Symbols Types into the trading platform
        /// </summary>        
        public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => new MessageSymbolTypes()
        {
            SymbolTypes = new List<SymbolType> { SymbolType.Crypto }
        };

        /// <summary>
        /// Provides a information about available Assets into the trading platform
        /// </summary>        
        public override IList<MessageAsset> GetAssets(CancellationToken token)
        {
            List<MessageAsset> result = new List<MessageAsset>();

            foreach (var item in this.symbolsCache)
            {
                var bitfinexSymbol = item.Value;

                if (bitfinexSymbol.Pair.Length == 6)
                {
                    var message = CreateMessageAsset(bitfinexSymbol.Pair.Substring(0, 3));
                    result.Add(message);

                    message = CreateMessageAsset(bitfinexSymbol.Pair.Substring(3, 3));
                    result.Add(message);
                }
                else
                {
                    int separatorIndex = bitfinexSymbol.Pair.IndexOf(BitfinexConsts.SYMBOL_SEPARATOR, StringComparison.Ordinal);

                    if (separatorIndex > 0)
                    {
                        var message = CreateMessageAsset(bitfinexSymbol.Pair.Substring(0, separatorIndex));
                        result.Add(message);

                        message = CreateMessageAsset(bitfinexSymbol.Pair.Substring(separatorIndex + 1, bitfinexSymbol.Pair.Length - separatorIndex - 1));
                        result.Add(message);
                    }
                    else
                        Core.Instance.Loggers.Log($"{BitfinexConsts.VENDOR_NAME}. Can't create assets for symbol {bitfinexSymbol.Pair}");
                }
            }

            return result;
        }

        /// <summary>
        /// Provides a information about available Exchanges into the trading platform
        /// </summary>        
        public override IList<MessageExchange> GetExchanges(CancellationToken token)
        {
            IList<MessageExchange> exchanges = new List<MessageExchange>
            {
                new MessageExchange()
                {
                    Id = BitfinexConsts.EXCHANGE_ID,
                    ExchangeName = "Exchange"
                }
            };

            return exchanges;
        }

        #endregion Symbols and symbol groups

        #region Accounts and rules

        /// <summary>
        /// Provides a information about available Rules into the trading platform
        /// </summary> 
        public override IList<MessageRule> GetRules(CancellationToken token)
        {
            var rules = base.GetRules(token);

            rules.Add(new MessageRule
            {
                Name = Rule.ALLOW_TRADING,
                Value = false
            });

            return rules;
        }
        #endregion Accounts and rules

        #region Subscriptions

        /// <summary>
        /// Called when the trading platform required subscription for quotes: level1, level2 or trades
        /// </summary>        
        public override void SubscribeSymbol(SubscribeQuotesParameters parameters)
        {
            switch (parameters.SubscribeType)
            {
                case SubscribeQuoteType.Quote:
                    this.socketApi.SubscribeTicker(parameters.SymbolId);
                    return;
                case SubscribeQuoteType.Level2:
                    this.socketApi.SubscribeBook(parameters.SymbolId);
                    return;
                case SubscribeQuoteType.Last:
                    this.socketApi.SubscribeTrades(parameters.SymbolId);
                    return;
            }
        }

        /// <summary>
        /// Called when the trading platform required unsubscription from quotes: level1, level2 or trades
        /// </summary>        
        public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters)
        {
            switch (parameters.SubscribeType)
            {
                case SubscribeQuoteType.Quote:
                    this.socketApi.UnsubscribeTicker(parameters.SymbolId);
                    return;
                case SubscribeQuoteType.Level2:
                    this.socketApi.UnsubscribeBook(parameters.SymbolId);
                    return;
                case SubscribeQuoteType.Last:
                    this.socketApi.UnsubscribeTrades(parameters.SymbolId);
                    return;
            }
        }

        #endregion Subscriptions

        #region History

        /// <summary>
        /// Provides an information about available history in this integration
        /// </summary>        
        public override HistoryMetadata GetHistoryMetadata(CancellationToken cancelationToken) => new HistoryMetadata()
        {
            AllowedHistoryTypes = new HistoryType[] { HistoryType.Last },
            DownloadingStep_Tick = TimeSpan.FromDays(10),
            AllowedPeriods = new Period[]
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
                new Period(BasePeriod.Day, 7),
                new Period(BasePeriod.Day, 14),
                Period.MONTH1
            }
        };

        /// <summary>
        /// Called when user requests history in the trading platform
        /// </summary>        
        public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters)
        {
            List<IHistoryItem> result = new List<IHistoryItem>();

            string symbol = requestParameters.SymbolId;

            long fromUnix = Core.Instance.TimeUtils.ConvertDateTimeToUnixMiliseconds(requestParameters.FromTime);
            long toUnix = Core.Instance.TimeUtils.ConvertDateTimeToUnixMiliseconds(requestParameters.ToTime);

            Stack<List<IHistoryItem>> itemsStack = new Stack<List<IHistoryItem>>();

            try
            {
                if (requestParameters.Period.BasePeriod == BasePeriod.Tick)
                {
                    long currentToUnix = toUnix;

                    while (fromUnix < currentToUnix)
                    {
                        var trades = this.restApi.GetTrades(symbol, fromUnix, currentToUnix).Result;

                        if (trades.Length == 0)
                            break;

                        List<IHistoryItem> ticks = new List<IHistoryItem>();
                        foreach(var trade in trades)
                        {
                            var last = CreateHistoryItemLast(trade);

                            ticks.Add(last);
                        }

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
                        var candles = this.restApi.GetCandles(symbol, timeFrame, fromUnix, currentToUnix).Result;

                        if (candles.Length == 0)
                            break;

                        List<IHistoryItem> bars = new List<IHistoryItem>();

                        for (int i = 0; i < candles.Length; i++)
                        {
                            var bar = CreateHistoryItemBar(candles[i]);

                            bars.Add(bar);
                        }

                        itemsStack.Push(bars);

                        currentToUnix = candles.Last().Timestamp - millisecondsInRequestPeriod;
                    }
                }
            }
            catch(Exception ex)
            {
                if (ex.InnerException is WebException wex && wex.Response is HttpWebResponse httpWebResponse && httpWebResponse.StatusCode == (HttpStatusCode)429)
                {
                    var dealTicket = DealTicketGenerator.CreateRefuseDealTicket($"{BitfinexConsts.VENDOR_NAME} request limit reached. Limit will be reseted in 1 minute");
                    PushMessage(dealTicket);
                }
            }
            finally
            {
                while(itemsStack.Count > 0)
                {
                    var items = itemsStack.Pop();

                    for (int i = items.Count - 1; i >= 0; i--)
                        result.Add(items[i]);
                }
            }

            return result;
        }
        #endregion History

        #region Factory
        private MessageAsset CreateMessageAsset(string assetName) => new MessageAsset
        {
            Id = assetName,
            Name = assetName,
            MinimumChange = 1e-8
        };

        private MessageSymbol CreateMessageSymbol(BitfinexSymbol bitfinexSymbol)
        {
            string baseAsset = bitfinexSymbol.Pair.Substring(0, 3);
            string quoteAsset = bitfinexSymbol.Pair.Substring(3, 3);

            var message = new MessageSymbol(bitfinexSymbol.Pair)
            {
                AllowCalculateRealtimeChange = false,
                AllowCalculateRealtimeVolume = false,
                AllowCalculateRealtimeTrades = false,
                AllowCalculateRealtimeTicks = false,
                AllowAbbreviatePriceByTickSize = true,
                Description = $"{baseAsset} vs {quoteAsset}",
                ExchangeId = BitfinexConsts.EXCHANGE_ID,
                HistoryType = HistoryType.Last,
                LotSize = 1,
                LotStep = Math.Pow(10, -8),
                NotionalValueStep = Math.Pow(10, -8),
                MinLot = bitfinexSymbol.MinimumOrderSize,
                MaxLot = bitfinexSymbol.MaximumOrderSize,
                Name = bitfinexSymbol.Pair,
                ProductAssetId = baseAsset,
                QuotingCurrencyAssetID = quoteAsset,
                SymbolType = SymbolType.Crypto,
                VolumeType = SymbolVolumeType.Volume,
                QuotingType = SymbolQuotingType.LotSize,
                DeltaCalculationType = DeltaCalculationType.TickDirection,
                VariableTickList = new List<VariableTick>
                {
                    new VariableTick(double.NegativeInfinity, double.PositiveInfinity, true, Math.Pow(10, -8), 1.0)
                }
            };

            message.SymbolAdditionalInfo = new List<AdditionalInfoItem>
            {
                new AdditionalInfoItem
                {
                    GroupInfo = BitfinexConsts.TRADING_INFO_GROUP,
                    SortIndex = 100,
                    Id = "Allow margin trading",
                    NameKey = loc.key("Allow margin trading"),
                    ToolTipKey = loc.key("Allow margin trading"),
                    DataType = ComparingType.String,
                    Value = bitfinexSymbol.AllowMargin,
                    Hidden = false
                },
            };

            if (bitfinexSymbol.AllowMargin)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
                {
                    GroupInfo = BitfinexConsts.TRADING_INFO_GROUP,
                    SortIndex = 110,
                    Id = "Initial margin",
                    NameKey = loc.key("Initial margin"),
                    ToolTipKey = loc.key("Initial margin"),
                    DataType = ComparingType.Double,
                    Value = bitfinexSymbol.InitialMargin,
                    Hidden = false
                });

                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
                {
                    GroupInfo = BitfinexConsts.TRADING_INFO_GROUP,
                    SortIndex = 120,
                    Id = "Minimum margin",
                    NameKey = loc.key("Minimum margin"),
                    ToolTipKey = loc.key("Minimum margin"),
                    DataType = ComparingType.Double,
                    Value = bitfinexSymbol.MinimumMargin,
                    Hidden = false
                });
            }

            return message;
        }

        private DayBar CreateDayBar(BitfinexTicker bitfinexTicker) => new DayBar(bitfinexTicker.Pair, Core.Instance.TimeUtils.DateTimeUtcNow)
        {
            Change = (double)bitfinexTicker.DailyChange,
            ChangePercentage = (double)(bitfinexTicker.DailyChangePercent * 100),
            High = (double)bitfinexTicker.High,
            Low = (double)bitfinexTicker.Low,
            Volume = (double)bitfinexTicker.Volume
        };

        private DayBar CreateDayBar(BitfinexTrade bitfinexTrade) => new DayBar(bitfinexTrade.Pair, Core.Instance.TimeUtils.ConvertUnixSecondsToDateTime((int)bitfinexTrade.Timestamp))
        {
            Last = (double)bitfinexTrade.Price,
            LastSize = (double)Math.Abs(bitfinexTrade.Amount)
        };

        private Quote CreateQuote(BitfinexTicker bitfinexTicker)
        {
            DateTime dateTime = Core.Instance.TimeUtils.DateTimeUtcNow;

            if (this.lastTradeTimeCache.TryGetValue(bitfinexTicker.Pair, out long lastTradeTime) && dateTime.Ticks <= lastTradeTime)
                dateTime = new DateTime(lastTradeTime + 1, DateTimeKind.Utc);

            this.lastTradeTimeCache[bitfinexTicker.Pair] = dateTime.Ticks;

            return new Quote(bitfinexTicker.Pair, (double)bitfinexTicker.Bid, 0, (double)bitfinexTicker.Ask, 0, dateTime);
        }

        private DOMQuote CreateDOMQuote(List<BitfinexBookItem> bitfinexBook)
        {
            DateTime utcNow = Core.Instance.TimeUtils.DateTimeUtcNow;
            string symbol = bitfinexBook.First().Pair;

            var bids = bitfinexBook.Where(b => b.Amount > 0).OrderBy(b => b.Price);
            var asks = bitfinexBook.Where(b => b.Amount < 0).OrderBy(b => b.Price);

            var dom = new DOMQuote(symbol, utcNow);

            foreach(var bid in bids)
            {
                string id = $"MMID_{bid.Price}";
                double price = (double)bid.Price;
                double size = bid.Count == 0 ? 0 : Math.Abs((double)bid.Amount);

                dom.Bids.Add(new Level2Quote(QuotePriceType.Bid, symbol, id, price, size, utcNow));
            }

            foreach (var ask in asks)
            {
                string id = $"MMID_{ask.Price}";
                double price = (double)ask.Price;
                double size = ask.Count == 0 ? 0 : Math.Abs((double)ask.Amount);

                dom.Asks.Add(new Level2Quote(QuotePriceType.Ask, symbol, id, price, size, utcNow));
            }

            return dom;
        }

        private Level2Quote CreateLevel2Quote(BitfinexBookItem bitfinexBookItem)
        {
            QuotePriceType priceType = bitfinexBookItem.Amount > 0 ? QuotePriceType.Bid : QuotePriceType.Ask;
            string symbol = bitfinexBookItem.Pair;
            string id = $"MMID_{bitfinexBookItem.Price}";
            double price = (double)bitfinexBookItem.Price;
            double size = bitfinexBookItem.Count == 0 ? 0 : Math.Abs((double)bitfinexBookItem.Amount);
            DateTime utcNow = Core.Instance.TimeUtils.DateTimeUtcNow;

            return new Level2Quote(priceType, symbol, id, price, size, utcNow);
        }

        private Last CreateLast(BitfinexTrade bitfinexTrade)
        {
            DateTime dateTime = Core.Instance.TimeUtils.ConvertUnixSecondsToDateTime((int)bitfinexTrade.Timestamp);

            if (this.lastTradeTimeCache.TryGetValue(bitfinexTrade.Pair, out long lastTradeTime) && dateTime.Ticks <= lastTradeTime)
                dateTime = new DateTime(lastTradeTime + 1, DateTimeKind.Utc);

            this.lastTradeTimeCache[bitfinexTrade.Pair] = dateTime.Ticks;

            return new Last(bitfinexTrade.Pair, (double)bitfinexTrade.Price, Math.Abs((double)bitfinexTrade.Amount), dateTime);
        }

        private IHistoryItem CreateHistoryItemLast(BitfinexTrade bitfinexTrade) => new HistoryItemLast
        {
            TicksLeft = Core.Instance.TimeUtils.ConvertUnixMilisecondsToDateTime(bitfinexTrade.Timestamp).Ticks,
            Price = (double)bitfinexTrade.Price,
            Volume = Math.Abs((double)bitfinexTrade.Amount)
        };

        private IHistoryItem CreateHistoryItemBar(BitfinexCandle bitfinexCandle) => new HistoryItemBar
        {
            TicksLeft = Core.Instance.TimeUtils.ConvertUnixMilisecondsToDateTime(bitfinexCandle.Timestamp).Ticks,
            Open = (double)bitfinexCandle.Open,
            High = (double)bitfinexCandle.High,
            Low = (double)bitfinexCandle.Low,
            Close = (double)bitfinexCandle.Close,
            Volume = (double)bitfinexCandle.Volume
        };
        #endregion Factory

        #region Misc

        private new void PushMessage(Message message)
        {
            this.NewMessage?.Invoke(message);

            base.PushMessage(message);
        }

        private string GetTimeFrameFromPeriod(Period period)
        {
            long ticks = period.Ticks;

            switch(ticks)
            {
                case TimeSpan.TicksPerMinute:
                    return "1m";
                case 5 * TimeSpan.TicksPerMinute:
                    return "5m";
                case 15 * TimeSpan.TicksPerMinute:
                    return "15m";
                case 30 * TimeSpan.TicksPerMinute:
                    return "30m";
                case TimeSpan.TicksPerHour:
                    return "1h";
                case 3 * TimeSpan.TicksPerHour:
                    return "3h";
                case 6 * TimeSpan.TicksPerHour:
                    return "6h";
                case 12 * TimeSpan.TicksPerHour:
                    return "12h";
                case TimeSpan.TicksPerDay:
                    return "1D";
                case 7 * TimeSpan.TicksPerDay:
                    return "7D";
                case 14 * TimeSpan.TicksPerDay:
                    return "14D";
                default:
                    return "1M";
            }
        }

        private void SocketApi_NewData(object sender, BitfinexEventArgs e)
        {
            if (e.Ticker != null)
            {
                var dayBar = CreateDayBar(e.Ticker);
                PushMessage(dayBar);

                var quote = CreateQuote(e.Ticker);

                this.aggressorFlagCalculator.CollectBidAsk(quote);

                PushMessage(quote);
            }

            if (e.Book != null)
            {
                var dom = CreateDOMQuote(e.Book);
                PushMessage(dom);
            }

            if (e.BookItem != null)
            {
                var level2 = CreateLevel2Quote(e.BookItem);
                PushMessage(level2);
            }

            if (e.Trade != null)
            {
                if (e.IsSnapshotData)
                {
                    var dayBar = CreateDayBar(e.Trade);
                    PushMessage(dayBar);
                }
                else
                {
                    var last = CreateLast(e.Trade);

                    last.AggressorFlag = this.aggressorFlagCalculator.CalculateAggressorFlag(last);

                    PushMessage(last);
                }
            }
        }

        #endregion Misc
    }
}
