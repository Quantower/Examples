// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.RateLimit;
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
using System.Text;
using System.Threading;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.Market;

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

    protected const string REQUESTS_TOO_FREQUENT_CODE = "50011";
    protected const int MAX_SEND_REQUEST_TRYING_COUNT = 3;

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

        if (this.settings.IsDemo)
            SetSimulatedTradingHeader(this.httpClient);

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
            this.PopulateOKExSymbolsCache(token, out error);

        return isConnected && string.IsNullOrEmpty(error);
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
        OKExRateLimitManager.Market.GetSymbols.WaitMyTurn(token);

        var responce = this.SendPublicGetRequest<OKExSymbol[]>($"{this.settings.RestEndpoint}/api/v5/public/instruments?instType={instrumentType.GetEnumMember()}", token);
        error = responce.Message;
        return responce.Data ?? new OKExSymbol[0];
    }
    internal OKExSymbol[] GetOptionsFor(string underlier, CancellationToken token, out string error)
    {
        OKExRateLimitManager.Market.GetSymbols.WaitMyTurn(token);

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
        var waiter = OKExRateLimitManager.Market.GetCandleHistory;

        if (symbol.InstrumentType == OKExInstrumentType.Index)
        {
            endpoint = "/api/v5/market/index-candles";
            waiter = OKExRateLimitManager.Market.GetIndexCandleHistory;
        }
        else if (historyType == OKExHistoryType.Last)
        {
            if (symbol.IsTopCurrency())
            {
                endpoint = "/api/v5/market/history-candles";
                waiter = OKExRateLimitManager.Market.GetTopCandleHistory;
            }
            else
            {
                endpoint = "/api/v5/market/candles";
                waiter = OKExRateLimitManager.Market.GetCandleHistory;
            }
        }
        else if (historyType == OKExHistoryType.Mark)
        {
            endpoint = "/api/v5/market/mark-price-candles";
            waiter = OKExRateLimitManager.Market.GetMarkCandleHistory;
        }

        //
        waiter?.WaitMyTurn(token);

        //
        var request = $"{this.settings.RestEndpoint}{endpoint}?instId={symbol.OKExInstrumentId}&bar={period.GetEnumMember()}";
        if (after.HasValue)
            request += $"&after={new DateTimeOffset(after.Value).ToUnixTimeMilliseconds()}";

        //
        var responce = this.SendPublicGetRequest<OKExCandleItem[]>(request, token);
        error = responce?.Message;
        return responce?.Data ?? new OKExCandleItem[0];
    }
    internal OKExTradeItem[] GetTickHistory(OKExSymbol symbol, DateTime from, DateTime to, CancellationToken token, out string error)
    {
        string innerErrorMessage = string.Empty;

        var items = this.DateTimePaginationLoaderWithRange(
            (afterDt) =>
            {
                OKExRateLimitManager.Market.GetTicksHistory.WaitMyTurn(token);

                var sb = new StringBuilder()
                    .Append(this.settings.RestEndpoint)
                    .Append("/api/v5/market/history-trades?")
                    .Append("&instId=").Append(symbol.OKExInstrumentId);

                if (afterDt != default)
                {
                    sb.Append("&type=2")
                      .Append("&after=").Append(afterDt.ToUnixMilliseconds());
                }
                var responce = this.SendPublicGetRequest<OKExTradeItem[]>(sb.ToString(), token);
                innerErrorMessage = responce.Message;
                return responce.Data ?? Array.Empty<OKExTradeItem>();
            },
            from,
            to, token);

        error = innerErrorMessage;
        return items;
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

    private void PopulateOKExSymbolsCache(CancellationToken token, out string error)
    {
        error = null;

        //
        var types = new[] { OKExInstrumentType.Spot, OKExInstrumentType.Futures, OKExInstrumentType.Swap };
        foreach (var item in types)
        {
            var symbols = this.GetSymbols(item, token, out error);

            if (token.IsCancellationRequested || !string.IsNullOrEmpty(error))
                return;

            this.symbolCache.AddSymbols(item, symbols);
        }

        //
        var underliers = this.symbolCache.GetSymbols(OKExInstrumentType.Futures).Select(i => i.Underlier).Distinct();
        foreach (var item in underliers)
        {
            var options = this.GetOptionsFor(item, token, out error);

            if (token.IsCancellationRequested || !string.IsNullOrEmpty(error))
                return;

            this.symbolCache.AddSymbols(OKExInstrumentType.Option, options);
        }

        this.symbolCache.PopulateIndexes();
    }
    private OKExRestResponce<T> SendPublicGetRequest<T>(string url, CancellationToken token, bool allowResendRequest = true, int tryCount = 0)
    {
        var responce = this.SendRestRequest<T>(() => this.httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token)?.Result, token);

        // check if response code 
        if (responce.Code == REQUESTS_TOO_FREQUENT_CODE && allowResendRequest && ++tryCount < MAX_SEND_REQUEST_TRYING_COUNT)
            return this.SendPublicGetRequest<T>(url, token, allowResendRequest, tryCount);
        //
        else
            return responce;
    }
    protected OKExRestResponce<T> SendRestRequest<T>(Func<HttpResponseMessage> getResponseHandler, CancellationToken token)
    {
        try
        {
            var httpResponce = getResponseHandler?.Invoke();

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

    protected T[] PaginationLoader<T>(Func<string, T[]> loadingFunction, int limit = 100) where T : IPaginationLoadingItem => this.PaginationLoaderWithRange(loadingFunction, DateTime.MinValue, DateTime.MaxValue, limit);
    protected T[] PaginationLoaderWithRange<T>(Func<string, T[]> loadingFunction, DateTime from, DateTime to, int limit = 100) where T : IPaginationLoadingItem
    {
        string afterId = null;

        var itemsCache = new List<T>();

        bool keepGoing = true;
        while (keepGoing)
        {
            var items = loadingFunction.Invoke(afterId);

            if (items == null || items.Length == 0)
                break;

            foreach (var item in items)
            {
                if (item.Time < from)
                {
                    keepGoing = false;
                    break;
                }

                if (item.Time <= to)
                    itemsCache.Add(item);
            }

            if (keepGoing && items.Length == limit)
            {
                if (items[0].Time > items[1].Time)
                    afterId = items[^1].AfterId;
                else
                    afterId = items[0].AfterId;
            }
            else
                keepGoing = false;
        }

        return itemsCache.ToArray();
    }
    protected T[] DateTimePaginationLoaderWithRange<T>(Func<DateTime, T[]> loadingFunction, DateTime from, DateTime to, CancellationToken token, int limit = 100) where T : IPaginationLoadingItem
    {
        var afterElement = to;

        var itemsCache = new List<T>();

        bool keepGoing = true;
        while (keepGoing)
        {
            if (token.IsCancellationRequested)
                break;

            var items = loadingFunction.Invoke(afterElement);

            if (items == null || items.Length == 0)
                break;

            foreach (var item in items)
            {
                if (item.Time < from)
                {
                    keepGoing = false;
                    break;
                }

                if (token.IsCancellationRequested)
                    break;

                if (item.Time <= to)
                    itemsCache.Add(item);
            }

            if (keepGoing && items.Length == limit)
            {
                if (items[0].Time > items[1].Time)
                    afterElement = items[^1].Time;
                else
                    afterElement = items[0].Time;

                afterElement = afterElement.AddMilliseconds(-1);
            }
            else
                keepGoing = false;
        }

        return itemsCache.ToArray();
    }

    protected static void SetSimulatedTradingHeader(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("x-simulated-trading", "1");
    }

    #endregion Misc
}