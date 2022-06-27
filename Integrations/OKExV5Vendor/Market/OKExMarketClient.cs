// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API;
using OKExV5Vendor.API.REST;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Subscriber;
using OKExV5Vendor.API.SymbolsCache;
using OKExV5Vendor.API.Websocket;
using OKExV5Vendor.API.Websocket.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace OKExV5Vendor.Market
{
    #region Market events

    internal delegate void OKExTradeReceiveEvent(OKExSymbol symbol, OKExTradeItem trade);
    internal delegate void OKExMarkReceiveEvent(OKExSymbol symbol, OKExMarkItem mark);
    internal delegate void OKExTickerReceiveEvent(OKExSymbol symbol, OKExTicker tiker, OKExOpenInterest oi, bool isFirstMessage);
    internal delegate void OKExIndexTickerReceiveEvent(OKExSymbol symbol, OKExIndexTicker tiker, bool isFirstMessage);
    internal delegate void OKExQuoteReceiveEvent(OKExSymbol symbol, IOKExQuote quote);
    internal delegate void OKExIndexPriceReceiveEvent(OKExSymbol symbol, IOKExIndexPrice indexPrice);
    internal delegate void OKExOrderBookReceiveEvent(OKExSymbol symbol, OKExOrderBook book);
    internal delegate void OKExFundingRateReceiveEvent(OKExFundingRate rate);
    internal delegate void OKExInstrumentChangedEvent(OKExSymbol symbol);

    #endregion Market events

    class OKExMarketClient
    {
        #region Parameters

        private readonly HttpClient httpClient;
        private readonly JsonSerializer jsonSerializer;
        private readonly OKExWebSocket publicWebsocket;

        private readonly IDictionary<string, OKExGeneralSubscriber> subscriberCache;
        private readonly IDictionary<string, OKExIndexSubscriber> indexSubscriberCache;

        protected readonly OKExClientSettings settings;

        private readonly OKExSymbolCache symbolCache;
        internal IOKExSymbolsProvider SymbolsProvider => this.symbolCache;

        internal virtual bool IsConnected => this.publicWebsocket.State == WebSocket4Net.WebSocketState.Open;
        internal virtual TimeSpan Ping => this.publicWebsocket.RoundTripTime;

        public string LastErrorMessage { get; protected set; }

        // events
        public event OKExTradeReceiveEvent OnNewTrade;
        public event OKExMarkReceiveEvent OnNewMark;
        public event OKExQuoteReceiveEvent OnNewQuote;
        public event OKExTickerReceiveEvent OnNewTicker;
        public event OKExIndexPriceReceiveEvent OnNewIndexPrice;
        public event OKExIndexTickerReceiveEvent OnNewIndexTicker;
        public event OKExOrderBookReceiveEvent OnOrderBookSnapshot;
        public event OKExOrderBookReceiveEvent OnOrderBookUpdate;
        public event OKExInstrumentChangedEvent OnInstrumentChanged;
        public event OKExFundingRateReceiveEvent OnFundingRateUpdated;
        public event Action<string> OnError;

        #endregion Parameters

        public OKExMarketClient(OKExClientSettings settings)
        {
            this.settings = settings;

            this.symbolCache = new OKExSymbolCache();

            this.httpClient = new HttpClient();
            this.jsonSerializer = new JsonSerializer();

            this.subscriberCache = new Dictionary<string, OKExGeneralSubscriber>();
            this.indexSubscriberCache = new Dictionary<string, OKExIndexSubscriber>();

            this.publicWebsocket = new OKExWebSocket(this.settings.PublicWebsoketEndpoint, useQueueRequest: true);
            this.publicWebsocket.OnResponceReceive += this.PublicWebsocket_OnResponceReceive;
        }

        #region Connect

        internal virtual bool Connect(CancellationToken token, out string error)
        {
            var isConnected = this.publicWebsocket.Connect(token, out error);

            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(this.LastErrorMessage))
                error = this.LastErrorMessage;

            if (isConnected)
                this.PopulateOKExSymbolsCache(token);

            return isConnected;
        }
        internal virtual void Disconnect()
        {
            this.subscriberCache.Clear();
            this.indexSubscriberCache.Clear();
            this.publicWebsocket.Disconnect();
        }

        #endregion Connect

        #region Subscription

        public void SubscribeLast(OKExSymbol symbol) => this.SubscribeSymbol(symbol, OKExSubscriptionType.Last);
        public void UnsubscribeLast(OKExSymbol symbol) => this.UnsubscribeSymbol(symbol, OKExSubscriptionType.Last);

        public void SubscribeQuote(OKExSymbol symbol) => this.SubscribeSymbol(symbol, OKExSubscriptionType.Quote);
        public void UnsubscribeQuote(OKExSymbol symbol) => this.UnsubscribeSymbol(symbol, OKExSubscriptionType.Quote);

        public void SubscribeLevel2(OKExSymbol symbol) => this.SubscribeSymbol(symbol, OKExSubscriptionType.Level2);
        public void UnsubscribeLevel2(OKExSymbol symbol) => this.UnsubscribeSymbol(symbol, OKExSubscriptionType.Level2);

        public void SubscribeMark(OKExSymbol symbol) => this.SubscribeSymbol(symbol, OKExSubscriptionType.Mark);
        public void UnsubscribeMark(OKExSymbol symbol) => this.UnsubscribeSymbol(symbol, OKExSubscriptionType.Mark);

        internal void SubscribeIndexPrice(OKExSymbol symbol)
        {
            if (symbol.InstrumentType != OKExInstrumentType.Index)
                return;

            if (!this.indexSubscriberCache.ContainsKey(symbol.OKExInstrumentId))
            {
                var subscriber = new OKExIndexSubscriber(symbol);
                subscriber.AddSubscription(OKExSubscriptionType.Ticker);

                this.indexSubscriberCache[symbol.OKExInstrumentId] = subscriber;

                if (this.TryGetChannelName(symbol, OKExSubscriptionType.Ticker, out string channelName))
                    this.publicWebsocket.AddRequestToQueue(new OKExChannelRequest() { ChannelName = channelName, InstrumentId = symbol.OKExInstrumentId });
            }
        }
        internal void UnsubscribeIndexPrice(OKExSymbol symbol)
        {
            if (symbol.InstrumentType != OKExInstrumentType.Index)
                return;

            if (this.indexSubscriberCache.TryGetValue(symbol.OKExInstrumentId, out var subscriber))
            {
                subscriber.RemoveChannel(OKExSubscriptionType.Ticker);
                this.indexSubscriberCache.Remove(symbol.OKExInstrumentId);

                if (this.TryGetChannelName(symbol, OKExSubscriptionType.Ticker, out string channelName))
                    this.publicWebsocket.RemoveFromQueue(new OKExChannelRequest() { ChannelName = channelName, InstrumentId = symbol.OKExInstrumentId });
            }
        }

        internal void SubscribeFundingRate()
        {
            var futures = this.SymbolsProvider.GetSymbols(OKExInstrumentType.Swap);

            var channels = new List<OKExChannelRequest>();
            foreach (var f in futures)
            {
                channels.Add(new OKExChannelRequest()
                {
                    InstrumentId = f.OKExInstrumentId,
                    ChannelName = OKExChannels.FUNDING_RATE
                });
            }

            this.publicWebsocket.AddRequestToQueue(channels.ToArray());
        }

        private void SubscribeSymbol(OKExSymbol symbol, OKExSubscriptionType type)
        {
            if (symbol.InstrumentType == OKExInstrumentType.Index)
                return;

            if (!this.subscriberCache.TryGetValue(symbol.OKExInstrumentId, out var okexSubscriber))
                this.subscriberCache[symbol.OKExInstrumentId] = okexSubscriber = new OKExGeneralSubscriber(symbol);

            var args = new List<OKExChannelRequest>();

            if (okexSubscriber.SubscriptionCount == 0)
            {
                okexSubscriber.AddSubscription(OKExSubscriptionType.Ticker);
                if (this.TryGetChannelName(symbol, OKExSubscriptionType.Ticker, out string channelName))
                    args.Add(new OKExChannelRequest() { ChannelName = channelName, InstrumentId = symbol.OKExInstrumentId });

                if (okexSubscriber.Symbol.InstrumentType != OKExInstrumentType.Spot)
                {
                    okexSubscriber.AddSubscription(OKExSubscriptionType.OpenInterest);
                    if (this.TryGetChannelName(symbol, OKExSubscriptionType.OpenInterest, out channelName))
                        args.Add(new OKExChannelRequest() { ChannelName = channelName, InstrumentId = symbol.OKExInstrumentId });
                }
            }

            if (!okexSubscriber.ContainsSubscription(type))
            {
                okexSubscriber.AddSubscription(type);

                if (this.TryGetChannelName(symbol, type, out string channelName))
                    args.Add(new OKExChannelRequest() { ChannelName = channelName, InstrumentId = symbol.OKExInstrumentId });

                if (args.Count > 0)
                    this.publicWebsocket.AddRequestToQueue(args.ToArray());
            }
        }
        private void UnsubscribeSymbol(OKExSymbol symbol, OKExSubscriptionType type)
        {
            if (symbol.InstrumentType == OKExInstrumentType.Index)
                return;

            if (!this.subscriberCache.TryGetValue(symbol.OKExInstrumentId, out var okexSubscriber))
                return;

            var channelArgs = new List<OKExChannelRequest>();
            okexSubscriber.RemoveChannel(type);

            if (this.TryGetChannelName(symbol, type, out string channelName))
            {
                channelArgs.Add(new OKExChannelRequest()
                {
                    ChannelName = channelName,
                    InstrumentId = symbol.OKExInstrumentId
                });
            }

            if (!okexSubscriber.ContainsAnyMainSubscription())
            {
                okexSubscriber.RemoveChannel(OKExSubscriptionType.Ticker);
                if (this.TryGetChannelName(symbol, OKExSubscriptionType.Ticker, out channelName))
                {
                    channelArgs.Add(new OKExChannelRequest()
                    {
                        ChannelName = channelName,
                        InstrumentId = symbol.OKExInstrumentId
                    });
                }

                if (symbol.InstrumentType != OKExInstrumentType.Spot)
                {
                    okexSubscriber.RemoveChannel(OKExSubscriptionType.OpenInterest);
                    if (this.TryGetChannelName(symbol, OKExSubscriptionType.OpenInterest, out channelName))
                    {
                        channelArgs.Add(new OKExChannelRequest()
                        {
                            ChannelName = channelName,
                            InstrumentId = symbol.OKExInstrumentId
                        });
                    }
                }

                this.subscriberCache.Remove(symbol.OKExInstrumentId);
            }

            if (channelArgs.Count > 0)
                this.publicWebsocket.RemoveFromQueue(channelArgs.ToArray());
        }

        #endregion Subscription

        #region Requests

        internal OKExSymbol[] GetSymbols(OKExInstrumentType instrumentType, CancellationToken token, out string error)
        {
            var responce = this.SendPublicGetRequest<OKExSymbol[]>($"{this.settings.RestEndpoint}/api/v5/public/instruments?instType={instrumentType.GetEnumMember()}", token);
            error = responce.Message;
            return responce.Data ?? new OKExSymbol[0];
        }
        internal OKExSymbol[] GetOptionsFor(string underlier, CancellationToken token, out string error)
        {
            var responce = this.SendPublicGetRequest<OKExSymbol[]>($"{this.settings.RestEndpoint}/api/v5/public/instruments?instType={OKExInstrumentType.Option.GetEnumMember()}&uly={underlier}", token);
            error = responce.Message;
            return responce.Data ?? new OKExSymbol[0];
        }
        internal OKExCandleItem[] GetCandleHistory(OKExSymbol symbol, DateTime? after, OKExCandlePeriod period, OKExHistoryType historyType, CancellationToken token, out string error)
        {
            if (period == OKExCandlePeriod.Tick1)
                throw new ArgumentException($"Unsupported candle period - {period}");

            // default
            string endpoint = "/api/v5/market/candles";

            if (symbol.InstrumentType == OKExInstrumentType.Index)
                endpoint = "/api/v5/market/index-candles";
            else if (historyType == OKExHistoryType.Last)
            {
                if (symbol.IsTopCurrency())
                    endpoint = "/api/v5/market/history-candles";
                else
                    endpoint = "/api/v5/market/candles";
            }
            else if (historyType == OKExHistoryType.Mark)
                endpoint = "/api/v5/market/mark-price-candles";

            //
            var request = $"{this.settings.RestEndpoint}{endpoint}?instId={symbol.OKExInstrumentId}&bar={period.GetEnumMember()}";
            if (after.HasValue)
                request += $"&after={new DateTimeOffset(after.Value).ToUnixTimeMilliseconds()}";

            //
            var responce = this.SendPublicGetRequest<OKExCandleItem[]>(request, token);
            error = responce?.Message;
            return responce?.Data ?? new OKExCandleItem[0];
        }
        internal OKExTradeItem[] GetTickHistory(OKExSymbol symbol, CancellationToken token, out string error)
        {
            var responce = this.SendPublicGetRequest<OKExTradeItem[]>($"{this.settings.RestEndpoint}/api/v5/market/trades?&instId={symbol.OKExInstrumentId}", token);
            error = responce.Message;
            return responce.Data ?? new OKExTradeItem[0];
        }
        internal string[] GetUnderlying(OKExInstrumentType instrumentType, CancellationToken token, out string error)
        {
            var responce = this.SendPublicGetRequest<string[][]>($"{this.settings.RestEndpoint}/api/v5/public/underlying?instType={instrumentType.GetEnumMember()}", token);
            error = responce.Message;
            return responce.Data.FirstOrDefault() ?? new string[0];
        }

        #endregion Requests

        #region Event handlers

        private void PublicWebsocket_OnResponceReceive(JObject jObject)
        {
            var arg = jObject.SelectToken("arg");
            var channelName = arg?.SelectToken("channel")?.ToString();

            switch (jObject.SelectToken("event")?.ToString())
            {
                case OKExConsts.ERROR:
                    {
                        if (jObject.SelectToken("msg")?.ToString() is string msg)
                        {
                            this.LastErrorMessage = msg;
                            this.CallOnErrorEvent(msg);
                        }
                        break;
                    }
            }

            string instrumentId = arg?.SelectToken("instId")?.ToString();
            if (jObject.SelectToken("data") is JToken data)
            {
                switch (channelName)
                {
                    case OKExChannels.TRADES:
                        {
                            if (this.subscriberCache.TryGetValue(instrumentId, out var subscriber))
                            {
                                var isFirstMessage = subscriber.LastTrade == null;

                                foreach (var t in data.ToObject<OKExTradeItem[]>())
                                {
                                    if (!isFirstMessage)
                                        this.OnNewTrade?.Invoke(subscriber.Symbol, t);

                                    subscriber.LastTrade = t;
                                }
                            }
                            break;
                        }
                    case OKExChannels.BBO_TBT:
                        {
                            if (this.subscriberCache.TryGetValue(instrumentId, out var subscriber))
                            {
                                var book = data.ToObject<OKExOrderBook[]>()?.FirstOrDefault();

                                if (book != null && book.Asks?.Length > 0 && book.Bids?.Length > 0)
                                {
                                    if (subscriber.TryUpdateQuote(ref book.Bids[0], ref book.Asks[0], book.Time, out var quote))
                                        this.OnNewQuote?.Invoke(subscriber.Symbol, quote);
                                }
                            }
                            break;
                        }
                    case OKExChannels.TICKERS:
                        {
                            if (this.subscriberCache.TryGetValue(instrumentId, out var subscriber))
                            {
                                foreach (var ticker in data.ToObject<OKExTicker[]>())
                                {
                                    var isFirstMessage = subscriber.LastTicker == null;

                                    if (subscriber.TryUpdateTicker(ticker))
                                        this.OnNewTicker(subscriber.Symbol, ticker, subscriber.OpenInterest, isFirstMessage);
                                }
                            }
                            break;
                        }
                    case OKExChannels.INDEX_TICKERS:
                        {
                            if (this.indexSubscriberCache.TryGetValue(instrumentId, out var subscriber))
                            {
                                foreach (var ticker in data.ToObject<OKExIndexTicker[]>())
                                {
                                    var isFirstMessage = subscriber.LastTicker == null;

                                    if (subscriber.TryUpdateTicker(ticker, out bool isPriceChanged))
                                        this.OnNewIndexTicker?.Invoke(subscriber.Symbol, ticker, isFirstMessage);

                                    if (isPriceChanged)
                                        this.OnNewIndexPrice?.Invoke(subscriber.Symbol, ticker);
                                }
                            }
                            break;
                        }
                    case OKExChannels.ORDER_BOOK_400:
                    case OKExChannels.ORDER_BOOK_400_TBT:
                        {
                            if (this.subscriberCache.TryGetValue(instrumentId, out var subscriber))
                            {
                                var book = data.ToObject<OKExOrderBook[]>()?.FirstOrDefault();
                                switch (jObject.SelectToken("action")?.ToString())
                                {
                                    case OKExConsts.ORDER_BOOK_SNAPSHOT:
                                        {
                                            if (book != null)
                                                this.OnOrderBookSnapshot?.Invoke(subscriber.Symbol, book);
                                            break;
                                        }
                                    case OKExConsts.ORDER_BOOK_UPDATE:
                                        {
                                            if (book != null)
                                                this.OnOrderBookUpdate?.Invoke(subscriber.Symbol, book);
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    case OKExChannels.MARK_PRICE:
                        {
                            if (this.subscriberCache.TryGetValue(instrumentId, out var subscriber))
                            {
                                foreach (var m in data.ToObject<OKExMarkItem[]>())
                                    this.OnNewMark?.Invoke(subscriber.Symbol, m);
                            }
                            break;
                        }
                    case OKExChannels.INSTRUMENTS:
                        {
                            var instType = arg.SelectToken("instType")?.ToObject<OKExInstrumentType>();

                            if (instType.HasValue)
                            {
                                var symbols = data.ToObject<OKExSymbol[]>();

                                // populate
                                if (!this.symbolCache.Contains(instType.Value))
                                    this.symbolCache.AddSymbols(instType.Value, symbols);
                                // update
                                else
                                {
                                    this.symbolCache.UpdateSymbols(symbols);

                                    foreach (var s in symbols)
                                        this.OnInstrumentChanged?.Invoke(s);
                                }
                            }

                            break;
                        }
                    case OKExChannels.FUNDING_RATE:
                        {
                            var rates = data.ToObject<OKExFundingRate[]>();

                            foreach (var rate in rates)
                                this.OnFundingRateUpdated?.Invoke(rate);
                            break;
                        }
                    case OKExChannels.OPEN_INTEREST:
                        {
                            var oi = data.ToObject<OKExOpenInterest[]>().FirstOrDefault();

                            if (oi != null && this.subscriberCache.TryGetValue(instrumentId, out var subscriber))
                                subscriber.OpenInterest = oi;

                            break;
                        }
                }
            }
        }

        #endregion Event handler

        #region Misc

        private void PopulateOKExSymbolsCache(CancellationToken token)
        {
            var request = new OKExSubscribeRequest()
            {
                Args = new OKExChannelRequest[]
                {
                    new OKExChannelRequest() { ChannelName = OKExChannels.INSTRUMENTS, InstrumentType = OKExInstrumentType.Spot },
                    new OKExChannelRequest() { ChannelName = OKExChannels.INSTRUMENTS, InstrumentType = OKExInstrumentType.Futures },
                    new OKExChannelRequest() { ChannelName = OKExChannels.INSTRUMENTS, InstrumentType = OKExInstrumentType.Option },
                    new OKExChannelRequest() { ChannelName = OKExChannels.INSTRUMENTS, InstrumentType = OKExInstrumentType.Swap }
                }
            };

            this.publicWebsocket.SendRequest(request);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!token.IsCancellationRequested && !cts.IsCancellationRequested && this.symbolCache.AvailableTypesCount != request.Args.Length)
                Thread.Sleep(100);

            this.symbolCache.PopulateIndexes();
        }
        private OKExRestResponce<T> SendPublicGetRequest<T>(string url, CancellationToken token)
        {
            try
            {
                var httpResponce = this.httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token)?.Result;

                using var stream = httpResponce.Content.ReadAsStreamAsync().Result;
                using var sr = new StreamReader(stream);
                using var jr = new JsonTextReader(sr);
                return this.jsonSerializer.Deserialize<OKExRestResponce<T>>(jr);
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested)
                    return new OKExRestResponce<T>();
                else
                {
                    Core.Instance.Loggers.Log(ex, connectionName: OKExConsts.VENDOR_NAME);
                    return new OKExRestResponce<T>() { Message = ex.Message };
                }
            }
        }
        protected void CallOnErrorEvent(string message) => this.OnError?.Invoke(message);

        protected virtual bool TryGetChannelName(OKExSymbol symbol, OKExSubscriptionType subscriptionType, out string channelName)
        {
            channelName = null;

            switch (subscriptionType)
            {
                case OKExSubscriptionType.Last:
                    channelName = OKExChannels.TRADES;
                    break;
                case OKExSubscriptionType.Mark:
                    channelName = OKExChannels.MARK_PRICE;
                    break;
                case OKExSubscriptionType.Level2:
                    channelName = OKExChannels.ORDER_BOOK_400;
                    break;
                case OKExSubscriptionType.Ticker:
                    {
                        if (symbol.InstrumentType == OKExInstrumentType.Index)
                            channelName = OKExChannels.INDEX_TICKERS;
                        else
                            channelName = OKExChannels.TICKERS;
                        break;
                    }
                case OKExSubscriptionType.Quote:
                    {
                        channelName = OKExChannels.BBO_TBT;
                        break;
                    }
                case OKExSubscriptionType.OpenInterest:
                    channelName = OKExChannels.OPEN_INTEREST;
                    break;
            }

            return channelName != null;

        }

        #endregion Misc
    }
}
