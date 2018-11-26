using HitBTC.Net;
using HitBTC.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace HitBTCVendor
{
    public class HitBTCVendor : Vendor
    {
        #region Properties

        private readonly HitRestApi restApi;
        private readonly HitSocketApi socketApi;

        private readonly Dictionary<string, HitCurrency> currenciesCache;
        private readonly Dictionary<string, HitSymbol> symbolsCache;

        private readonly Ping ping;
        private readonly Uri pingUri;

        private readonly Dictionary<string, long> lastsTimeCache;

        private readonly AggressorFlagCalculator aggressorFlagCalculator;
        #endregion Properties

        public HitBTCVendor()
        {
            this.restApi = new HitRestApi();
            this.socketApi = new HitSocketApi();
            this.socketApi.ConnectionStateChanged += this.SocketApi_ConnectionStateChanged;
            this.socketApi.Notification += this.SocketApi_Notification;

            this.currenciesCache = new Dictionary<string, HitCurrency>();
            this.symbolsCache = new Dictionary<string, HitSymbol>();

            this.ping = new Ping();
            this.pingUri = new Uri("https://api.hitbtc.com/api/2");

            this.lastsTimeCache = new Dictionary<string, long>();

            this.aggressorFlagCalculator = new AggressorFlagCalculator();
        }

        #region Integration details
        public override VendorMetaData GetVendorMetaData() => new VendorMetaData
        {
            VendorName = HitBTCConsts.VENDOR_NAME,
            VendorDescription = "Market data connection. Trading coming soon."
        };
        #endregion Integration details

        #region Connection

        public override IList<ConnectionInfo> GetDefaultConnections() => new List<ConnectionInfo>
        {
            this.CreateDefaultConnectionInfo("HitBTC", HitBTCConsts.VENDOR_NAME, "HitBTCVendor\\hit_btc.svg", allowCreateCustomConnections: false)
        };

        public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return ConnectionResult.CreateFail("Network does not available");

            var token = connectRequestParameters.CancellationToken;

            this.socketApi.ConnectAsync().Wait(token);

            while(this.socketApi.ConnectionState != HitConnectionState.Connected)
            {
                if (token.IsCancellationRequested)
                    return ConnectionResult.CreateCancelled();

                if (this.socketApi.ConnectionState != HitConnectionState.Connecting)
                    break;

                Task.Delay(100);
            }

            if (this.socketApi.ConnectionState != HitConnectionState.Connected)
                return ConnectionResult.CreateFail("Can't connect to socket API");

            var currencies = CheckHitResponse(this.restApi.GetCurrencies(token).Result, out HitError hitError);
            if (hitError != null)
                return ConnectionResult.CreateFail(hitError.Format());

            foreach (var currency in currencies)
                this.currenciesCache.Add(currency.Id, currency);

            var symbols = CheckHitResponse(this.restApi.GetSymbols(token).Result, out hitError);
            if (hitError != null)
                return ConnectionResult.CreateFail(hitError.Format());

            foreach (var symbol in symbols)
                this.symbolsCache.Add(symbol.Id, symbol);

            return ConnectionResult.CreateSuccess();
        }

        public override void Disconnect()
        {
            this.socketApi.ConnectionStateChanged -= this.SocketApi_ConnectionStateChanged;
            this.socketApi.Notification -= this.SocketApi_Notification;
            this.socketApi.DisconnectAsync();

            this.lastsTimeCache.Clear();

            this.aggressorFlagCalculator.Dispose();
        }

        public override PingResult Ping()
        {
            var result = new PingResult
            {
                State = PingEnum.Disconnected
            };

            if (this.socketApi.ConnectionState != HitConnectionState.Connected)
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
                    Core.Instance.Loggers.Log($"{HitBTCConsts.VENDOR_NAME}. Error while pinging host: {this.pingUri.Host}");
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }

            return result;
        }

        #endregion Connection

        #region Symbols and symbol groups         
        public override IList<MessageSymbol> GetSymbols()
        {
            List<MessageSymbol> result = new List<MessageSymbol>();

            foreach (var item in this.symbolsCache)
            {
                var message = CreateMessageSymbol(item.Value);

                result.Add(message);

                this.lastsTimeCache.Add(item.Key, 0);
            }

            return result;
        }

        public override MessageSymbolTypes GetSymbolTypes() => new MessageSymbolTypes()
        {
            SymbolTypes = new List<SymbolType> { SymbolType.Crypto }
        };

        public override IList<MessageAsset> GetAssets()
        {
            List<MessageAsset> result = new List<MessageAsset>();

            foreach(var item in this.currenciesCache)
            {
                var message = CreateMessageAsset(item.Value);

                result.Add(message);
            }

            return result;
        }

        public override IList<MessageExchange> GetExchanges()
        {
            IList<MessageExchange> exchanges = new List<MessageExchange>
            {
                new MessageExchange()
                {
                    Id = HitBTCConsts.EXCHANGE_ID,
                    ExchangeName = "Exchange"
                }
            };

            return exchanges;
        }
        #endregion

        #region Accounts and rules
        public override IList<MessageRule> GetRules()
        {
            var rules = base.GetRules();

            rules.Add(new MessageRule
            {
                Name = Rule.ALLOW_TRADING,
                Value = false
            });

            return rules;
        }
        #endregion Accounts and rules

        #region Subscriptions
        public override void SubscribeSymbol(SubscribeQuotesParameters parameters)
        {
            switch (parameters.SubscribeType)
            {
                case SubscribeQuoteType.Quote:
                    this.socketApi.SubscribeTickerAsync(parameters.SymbolId).Wait();
                    break;
                case SubscribeQuoteType.Level2:
                    this.socketApi.SubscribeOrderbookAsync(parameters.SymbolId).Wait();
                    break;
                case SubscribeQuoteType.Last:
                    this.socketApi.SubscribeTradesAsync(parameters.SymbolId).Wait();
                    break;
            }

            System.Threading.Thread.Sleep(100);
        }

        public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters)
        {
            switch (parameters.SubscribeType)
            {
                case SubscribeQuoteType.Quote:
                    this.socketApi.UnsubscribeTickerAsync(parameters.SymbolId);
                    break;
                case SubscribeQuoteType.Level2:
                    this.socketApi.UnsubscribeOrderbookAsync(parameters.SymbolId);
                    break;
                case SubscribeQuoteType.Last:
                    this.socketApi.UnsubscribeTradesAsync(parameters.SymbolId);
                    break;
            }
        }
        #endregion Subscriptions

        #region History
        public override HistoryMetadata GetHistoryMetadata() => new HistoryMetadata()
        {
            AllowedHistoryTypes = new HistoryType[] { HistoryType.Last },
            AllowedPeriods = new Period[]
            {
                Period.TICK1,
                Period.MIN1,
                Period.MIN3,
                Period.MIN5,
                Period.MIN15,
                Period.MIN30,
                Period.HOUR1,
                Period.HOUR4,
                Period.DAY1,
                new Period(BasePeriod.Day, 7),
                Period.MONTH1
            }
        };

        public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters)
        {
            List<IHistoryItem> result = new List<IHistoryItem>();

            string symbol = requestParameters.Symbol.Id;
            DateTime from = requestParameters.FromTime;
            DateTime to = requestParameters.ToTime;
            var token = requestParameters.CancellationToken;

            if (requestParameters.Period.BasePeriod == BasePeriod.Tick)
            {
                List<HitTrade> hitTrades = new List<HitTrade>();

                while (from < to)
                {
                    var trades = CheckHitResponse(this.restApi.GetTradesByTimestamp(symbol, HitSort.Asc, from, to, 1000, cancellationToken: token).Result, out HitError hitError);

                    if (hitError != null || trades.Length == 0 || token.IsCancellationRequested)
                        break;

                    hitTrades.AddRange(trades);

                    from = trades.Last().Timestamp.AddSeconds(1);
                }


                long prevTimeTicks = 0;
                foreach (var hitTrade in hitTrades)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var last = CreateHistoryItem(hitTrade);

                    if (last.TicksLeft <= prevTimeTicks)
                        last.TicksLeft = prevTimeTicks + 1;

                    prevTimeTicks = last.TicksLeft;

                    result.Add(last);
                }
            }
            else
            {
                var hitPeriod = ConvertPeriod(requestParameters.Period);

                var candles = CheckHitResponse(this.restApi.GetCandles(symbol, hitPeriod, 1000, token).Result, out HitError hitError);
                if (hitError == null)
                {
                    foreach(var candle in candles)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (candle.Timestamp < from || candle.Timestamp > to)
                            continue;

                        var bar = CreateHistoryItem(candle);

                        result.Add(bar);
                    }
                }
            }

            return result;
        }
        #endregion History

        #region Factory
        private MessageAsset CreateMessageAsset(HitCurrency hitCurrency) => new MessageAsset
        {
            Id = hitCurrency.Id,
            Name = hitCurrency.Id,
            MinimumChange = 1e-8
        };

        private MessageSymbol CreateMessageSymbol(HitSymbol hitSymbol)
        {
            var message = new MessageSymbol(hitSymbol.Id)
            {
                Name = hitSymbol.Id,
                ProductAssetId = hitSymbol.BaseCurrency,
                QuotingCurrencyAssetID = hitSymbol.QuoteCurrency,
                SymbolType = SymbolType.Crypto,
                ExchangeId = HitBTCConsts.EXCHANGE_ID,
                HistoryType = HistoryType.Last,
                VolumeType = SymbolVolumeType.Volume,
                NettingType = NettingType.Undefined,
                QuotingType = SymbolQuotingType.LotSize,
                DeltaCalculationType = DeltaCalculationType.TickDirection,
                VariableTickList = new List<VariableTick>
                {
                    new VariableTick(double.NegativeInfinity, double.PositiveInfinity, true, (double)hitSymbol.TickSize, 1.0)
                },
                LotSize = 1d,
                LotStep = (double)hitSymbol.QuantityIncrement,
                NotionalValueStep = (double)hitSymbol.QuantityIncrement,
                MinLot = (double)hitSymbol.QuantityIncrement,
                MaxLot = int.MaxValue,

                AllowCalculateRealtimeChange = false,
                AllowCalculateRealtimeVolume = false,
                AllowCalculateRealtimeTicks = false,
                AllowCalculateRealtimeTrades = false,

                SymbolAdditionalInfo = new List<SymbolAdditionalInfoItem>
                {
                    new SymbolAdditionalInfoItem
                    {
                        GroupInfo = HitBTCConsts.TRADING_INFO_GROUP,
                        SortIndex = 100,
                        APIKey = "Take liquidity rate",
                        NameKey = loc.key("Take liquidity rate"),
                        ToolTipKey = loc.key("Take liquidity rate"),
                        DataType = ComparingType.Double,
                        Value = (double)hitSymbol.TakeLiquidityRate,
                        Hidden = false
                    },
                    new SymbolAdditionalInfoItem
                    {
                        GroupInfo = HitBTCConsts.TRADING_INFO_GROUP,
                        SortIndex = 110,
                        APIKey = "Provide liquidity rate",
                        NameKey = loc.key("Provide liquidity rate"),
                        ToolTipKey = loc.key("Provide liquidity rate"),
                        DataType = ComparingType.Double,
                        Value = (double)hitSymbol.ProvideLiquidityRate,
                        Hidden = false
                    },
                    new SymbolAdditionalInfoItem
                    {
                        GroupInfo = HitBTCConsts.TRADING_INFO_GROUP,
                        SortIndex = 120,
                        APIKey = "Fee currency",
                        NameKey = loc.key("Fee currency"),
                        ToolTipKey = loc.key("Fee currency"),
                        DataType = ComparingType.String,
                        Value = hitSymbol.FeeCurrency,
                        Hidden = false
                    }
                }
            };

            if (this.currenciesCache.TryGetValue(hitSymbol.BaseCurrency, out HitCurrency baseCurrency)
                && this.currenciesCache.TryGetValue(hitSymbol.QuoteCurrency, out HitCurrency quoteCurrency))
                message.Description = $"{baseCurrency.FullName} vs {quoteCurrency.FullName}";

            return message;
        }

        private IHistoryItem CreateHistoryItem(HitCandle hitCandle) => new HistoryItemBar
        {
            TicksLeft = hitCandle.Timestamp.Ticks,
            Open = (double)hitCandle.Open,
            High = (double)hitCandle.Max,
            Low = (double)hitCandle.Min,
            Close = (double)hitCandle.Close,
            Volume = (double)hitCandle.VolumeQuote
        };

        private IHistoryItem CreateHistoryItem(HitTrade hitTrade) => new HistoryItemLast
        {
            TicksLeft = hitTrade.Timestamp.Ticks,
            Price = (double)hitTrade.Price,
            Volume = (double)(hitTrade.Price * hitTrade.Quantity)
        };

        private Quote CreateQuote(HitTicker hitTicker)
        {
            string symbol = hitTicker.Symbol;
            double bid = hitTicker.Bid.HasValue ? (double)hitTicker.Bid : double.NaN;
            double ask = hitTicker.Ask.HasValue ? (double)hitTicker.Ask : double.NaN;
            DateTime dateTime = hitTicker.Timestamp;
            return new Quote(symbol, bid, 0, ask, 0, dateTime);
        }

        private DOMQuote CreateDOMQuote(HitOrderBookData hitOrderBookData)
        {
            string symbol = hitOrderBookData.Symbol;
            var utcNow = Core.Instance.TimeUtils.DateTimeUtcNow;

            var dom = new DOMQuote(symbol, utcNow);

            dom.Bids.AddRange(CreateLevel2Quotes(QuotePriceType.Bid, hitOrderBookData.Bids, symbol, utcNow));

            dom.Asks.AddRange(CreateLevel2Quotes(QuotePriceType.Ask, hitOrderBookData.Asks, symbol, utcNow));

            return dom;
        }

        private List<Level2Quote> CreateLevel2Quotes(HitOrderBookData hitOrderBookData)
        {
            var quotes = new List<Level2Quote>();

            string symbol = hitOrderBookData.Symbol;
            var utcNow = Core.Instance.TimeUtils.DateTimeUtcNow;

            quotes.AddRange(CreateLevel2Quotes(QuotePriceType.Bid, hitOrderBookData.Bids, symbol, utcNow));

            quotes.AddRange(CreateLevel2Quotes(QuotePriceType.Ask, hitOrderBookData.Asks, symbol, utcNow));

            return quotes;
        }

        private IEnumerable<Level2Quote> CreateLevel2Quotes(QuotePriceType priceType, HitLevel2[] hitLevels, string symbol, DateTime dateTime)
        {
            foreach (var item in hitLevels)
            {
                string id = $"MMID_{item.Price}";
                double price = (double)item.Price;
                double size = (double)item.Size;

                yield return new Level2Quote(priceType, symbol, id, price, size, dateTime);
            }
        }

        private IEnumerable<Last> CreateLasts(HitTradesData hitTradesData)
        {
            string symbol = hitTradesData.Symbol;
            this.lastsTimeCache.TryGetValue(symbol, out long lastTimeTicks);

            foreach (var item in hitTradesData.Data)
            {
                DateTime dateTime = item.Timestamp;
                double price = (double)item.Price;
                double size = (double)(item.Price * item.Quantity);

                if (dateTime.Ticks <= lastTimeTicks)
                    dateTime = new DateTime(++lastTimeTicks, DateTimeKind.Utc);
                else
                    lastTimeTicks = dateTime.Ticks;

                yield return new Last(symbol, price, size, dateTime);
            }

            this.lastsTimeCache[symbol] = lastTimeTicks;
        }

        private DayBar CreateDayBar(HitTicker hitTicker)
        {
            var dayBar = new DayBar(hitTicker.Symbol, hitTicker.Timestamp);

            if (hitTicker.Last.HasValue && hitTicker.Open.HasValue)
            {
                dayBar.Change = (double)(hitTicker.Last - hitTicker.Open);
                dayBar.ChangePercentage = (double)((hitTicker.Last - hitTicker.Open) / hitTicker.Open) * 100;
                dayBar.Open = (double)hitTicker.Open;
            }

            if (hitTicker.High.HasValue)
                dayBar.High = (double)hitTicker.High;

            if (hitTicker.Low.HasValue)
                dayBar.Low = (double)hitTicker.Low;

            if (hitTicker.Volume.HasValue)
                dayBar.Volume = (double)hitTicker.Volume;

            return dayBar;
        }

        private DayBar CreateDayBar(HitTradesData hitTradesData)
        {
            var lastTrade = hitTradesData.Data.Last();

            return new DayBar(hitTradesData.Symbol, lastTrade.Timestamp)
            {
                Last = (double)lastTrade.Price,
                LastSize = (double)(lastTrade.Price * lastTrade.Quantity)
            };
        }

        private HitPeriod ConvertPeriod(Period period)
        {
            if (period == Period.MIN1)
                return HitPeriod.Minute1;
            if (period == Period.MIN3)
                return HitPeriod.Minute3;
            if (period == Period.MIN5)
                return HitPeriod.Minute5;
            if (period == Period.MIN15)
                return HitPeriod.Minute15;
            if (period == Period.MIN30)
                return HitPeriod.Minute30;
            if (period == Period.HOUR1)
                return HitPeriod.Hour1;
            if (period == Period.HOUR4)
                return HitPeriod.Hour4;
            if (period == Period.DAY1)
                return HitPeriod.Day1;
            if (period == new Period(BasePeriod.Day, 7))
                return HitPeriod.Day7;
            if (period == Period.MONTH1)
                return HitPeriod.Month1;

            return HitPeriod.Minute1;
        }
        #endregion Factory

        #region Misc
        private T CheckHitResponse<T>(HitResponse<T> hitResponse, out HitError hitError)
        {
            T result = hitResponse.Result;
            hitError = hitResponse.Error;

            return result;
        }

        #endregion Misc

        private void SocketApi_Notification(HitSocketApi hitSocketApi, HitEventArgs e)
        {
            if (e.SocketError != null)
            {
                PushMessage(DealTicketGenerator.CreateRefuseDealTicket(e.SocketError.Message));
                Core.Instance.Loggers.Log(e.SocketError);
                return;
            }
           
            switch(e.Notification)
            {
                case HitNotification<HitTicker> ticker:
                    var quote = CreateQuote(ticker.Params);
                    this.aggressorFlagCalculator.CollectBidAsk(quote);
                    PushMessage(quote);

                    var dayBar = CreateDayBar(ticker.Params);
                    PushMessage(dayBar);
                    break;
                case HitNotification<HitOrderBookData> orderBookData:
                    if (orderBookData.Method == HitNotificationMethod.SnapshotOrderBook)
                    {
                        var dom = CreateDOMQuote(orderBookData.Params);
                        PushMessage(dom);
                    }
                    else
                    {
                        var level2List = CreateLevel2Quotes(orderBookData.Params);
                        level2List.ForEach(l2 => PushMessage(l2));
                    }
                    break;
                case HitNotification<HitTradesData> tradesData:
                    if (tradesData.Method == HitNotificationMethod.SnapshotTrades)
                    {
                        dayBar = CreateDayBar(tradesData.Params);
                        PushMessage(dayBar);
                    }
                    else
                    {
                        foreach (var last in CreateLasts(tradesData.Params))
                        {
                            last.AggressorFlag = this.aggressorFlagCalculator.CalculateAggressorFlag(last);
                            PushMessage(last);
                        }
                    }
                    break;
            }
        }

        private void SocketApi_ConnectionStateChanged(HitSocketApi hitSocketApi, HitEventArgs e)
        {
            if (e.SocketError != null)
            {
                var dealTicket = DealTicketGenerator.CreateRefuseDealTicket(e.SocketError.Message);
                PushMessage(dealTicket);
            }
        }
    }
}