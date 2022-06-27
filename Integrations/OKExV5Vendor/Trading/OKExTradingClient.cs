// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.RateLimit;
using OKExV5Vendor.API.REST;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Subscriber;
using OKExV5Vendor.API.Websocket;
using OKExV5Vendor.API.Websocket.Models;
using OKExV5Vendor.Market;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.Trading
{
    internal class OKExTradingClient : OKExMarketClient
    {
        #region Parameters

        private readonly string secret;
        private readonly string apiKey;
        private readonly string passPhrase;

        private readonly HttpClient httpClient;
        private readonly JsonSerializer jsonSerializer;
        private readonly OKExWebSocket privateWebsocket;

        private readonly IDictionary<OKExInstrumentType, OKExFeeRate> feeRatesCache;

        private readonly HashSet<string> subscribedTradingChannels;
        private readonly object sendPrivateRequestLockKey = new();

        internal bool IsLogged { get; private set; }

        internal override bool IsConnected => base.IsConnected && this.privateWebsocket.State == WebSocket4Net.WebSocketState.Open;

        // event
        public event Action<OKExBalance> OnBalanceChanged;
        public event Action<OKExOrder> OnOrder;
        public event Action<OKExAlgoOrder> OnAlgoOrder;
        public event Action<OKExPosition> OnPosition;

        #endregion Parameters

        public OKExTradingClient(string apiKey, string secret, string passPhrase, OKExClientSettings settings)
            : base(settings)
        {
            this.secret = secret;
            this.apiKey = apiKey;
            this.passPhrase = passPhrase;
            this.subscribedTradingChannels = new HashSet<string>();
            this.feeRatesCache = new Dictionary<OKExInstrumentType, OKExFeeRate>();

            this.LastErrorMessage = string.Empty;
            this.jsonSerializer = new JsonSerializer();

            this.privateWebsocket = new OKExWebSocket(this.settings.PrivateWebsoketEndpoint, useQueueRequest: false);
            this.privateWebsocket.OnResponceReceive += this.PrivateWebsocket_OnResponceReceive;

            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", apiKey);
            this.httpClient.DefaultRequestHeaders.Add("OK-ACCESS-PASSPHRASE", passPhrase);
        }

        #region Connect

        internal override bool Connect(CancellationToken token, out string error)
        {
            var isConnected = base.Connect(token, out error) &&
                              this.privateWebsocket.Connect(token, out error) &&
                              this.SendAuthWebsocketRequest(token) &&
                              this.SubscribeToTradingChannels(token);

            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(this.LastErrorMessage))
                error = this.LastErrorMessage;

            //
            if (isConnected)
            {
                var instTypes = new OKExInstrumentType[] { OKExInstrumentType.Spot, OKExInstrumentType.Margin, OKExInstrumentType.Futures, OKExInstrumentType.Option, OKExInstrumentType.Swap };

                foreach (var item in instTypes)
                {
                    var feeRate = this.GetFeeRate(item, token, out error);

                    if (feeRate != null)
                        this.feeRatesCache[item] = feeRate;
                }
            }

            return isConnected;
        }
        internal override void Disconnect()
        {
            this.privateWebsocket.Disconnect();
            base.Disconnect();
        }

        #endregion Connect

        #region Data requests

        internal OKExLeverage[] GetLeverage(OKExSymbol symbol, OKExTradeMode mode, CancellationToken token, out string error)
        {
            var responce = this.SendPrivateGetRequest<OKExLeverage[]>(this.settings.RestEndpoint, $"/api/v5/account/leverage-info?instId={symbol.OKExInstrumentId}&mgnMode={mode.GetEnumMember()}", token);
            error = responce.Message;
            return responce.Data ?? new OKExLeverage[0];
        }
        internal OKExBalance[] GetBalance(CancellationToken token, out string error)
        {
            var responce = this.SendPrivateGetRequest<OKExBalance[]>(this.settings.RestEndpoint, "/api/v5/account/balance", token);
            error = responce.Message;
            return responce.Data ?? new OKExBalance[0];
        }
        internal OKExOrder[] GetOrdersList(CancellationToken token, out string error)
        {
            string innerErrorMessage = string.Empty;

            var items = this.PaginationLoader((afterId) => this.GetOrdersList(token, afterId, out innerErrorMessage));
            error = innerErrorMessage;

            return items;
        }
        internal OKExOrder[] GetOrdersList(CancellationToken token, string afterAlgoId, out string error)
        {
            var requestPath = $"/api/v5/trade/orders-pending";

            if (!string.IsNullOrEmpty(afterAlgoId))
                requestPath += $"?after={afterAlgoId}";

            var responce = this.SendPrivateGetRequest<OKExOrder[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExOrder[0];
        }
        internal OKExAlgoOrder[] GetAlgoOrdersList(CancellationToken token, out string error)
        {
            error = null;
            var algoOrderTypes = new OKExAlgoOrderType[] { OKExAlgoOrderType.Conditional, OKExAlgoOrderType.OCO, OKExAlgoOrderType.Trigger };
            var orders = new List<OKExAlgoOrder>();

            foreach (var type in algoOrderTypes)
            {
                var items = this.GetAlgoOrdersList(type, token, out error);

                if (items == null)
                    break;

                orders.AddRange(items);
            }

            return orders.ToArray();
        }
        internal OKExAlgoOrder[] GetAlgoOrdersList(OKExAlgoOrderType orderType, CancellationToken token, out string error)
        {
            var innerErrorMessage = string.Empty;

            var items = this.PaginationLoader((afterId) => this.GetAlgoOrdersList(orderType, afterId, token, out innerErrorMessage));
            error = innerErrorMessage;

            return items;
        }
        internal OKExAlgoOrder[] GetAlgoOrdersList(OKExAlgoOrderType orderType, string afterAlgoId, CancellationToken token, out string error)
        {
            var requestPath = $"/api/v5/trade/orders-algo-pending?ordType={orderType.GetEnumMember()}";

            if (!string.IsNullOrEmpty(afterAlgoId))
                requestPath += $"&after={afterAlgoId}";

            var responce = this.SendPrivateGetRequest<OKExAlgoOrder[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExAlgoOrder[0];
        }
        internal OKExPosition[] GetPositions(CancellationToken token, out string error)
        {
            var responce = this.SendPrivateGetRequest<OKExPosition[]>(this.settings.RestEndpoint, "/api/v5/account/positions", token);
            error = responce.Message;
            return responce.Data ?? new OKExPosition[0];
        }
        internal OKExAccount GetAccount(CancellationToken token, out string error)
        {
            var responce = this.SendPrivateGetRequest<OKExAccount[]>(this.settings.RestEndpoint, "/api/v5/account/config", token);
            error = responce.Message;
            return responce.Data?.FirstOrDefault();
        }
        internal OKExFeeRate GetFeeRate(OKExInstrumentType type, CancellationToken token, out string error)
        {
            var responce = this.SendPrivateGetRequest<OKExFeeRate[]>(this.settings.RestEndpoint, $"/api/v5/account/trade-fee?instType={type.GetEnumMember()}", token);
            error = responce.Message;
            return responce.Data?.FirstOrDefault();
        }
        internal OKExOrder[] GetHistoryOrders(OKExSymbol okexSymbol, DateTime fromDateTime, DateTime toDateTime, OKExOrderState? state, CancellationToken token, out string error)
        {
            return this.GetHistoryOrders(okexSymbol, okexSymbol.InstrumentType, state, fromDateTime, toDateTime, token, out error);
        }
        internal OKExOrder[] GetHistoryOrders(OKExInstrumentType type, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            return this.GetHistoryOrders(null, type, null, fromDateTime, toDateTime, token, out error);
        }
        private OKExOrder[] GetHistoryOrders(OKExSymbol okexSymbol, OKExInstrumentType? type, OKExOrderState? state, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            string innerErrorMessage = string.Empty;

            var items = this.PaginationLoaderWithRange(
                (afterId) => this.GetHistoryOrders(okexSymbol, type, state, afterId, token, out innerErrorMessage),
                fromDateTime,
                toDateTime);

            if (type == OKExInstrumentType.Spot)
            {
                var items2 = this.PaginationLoaderWithRange(
                    (afterId) => this.GetHistoryOrders(okexSymbol, OKExInstrumentType.Margin, state, afterId, token, out innerErrorMessage),
                    fromDateTime,
                    toDateTime);

                items = Enumerable.Concat(items, items2).ToArray();
            }

            error = innerErrorMessage;
            return items;
        }
        private OKExOrder[] GetHistoryOrders(OKExSymbol okexSymbol, OKExInstrumentType? type, OKExOrderState? state, string afterId, CancellationToken token, out string error)
        {
            OKExRateLimitManager.OrdersHistory.WaitMyTurn();

            if (type == OKExInstrumentType.Index || type == OKExInstrumentType.Any)
            {
                error = "Unsupported instrument type";
                return new OKExOrder[0];
            }

            var parameters = new List<string>();

            if (okexSymbol != null)
                parameters.Add($"instId={okexSymbol.OKExInstrumentId}");

            if (state.HasValue)
                parameters.Add($"state={state.GetEnumMember()}");

            if (type.HasValue)
                parameters.Add($"instType={type.GetEnumMember()}");

            if (!string.IsNullOrEmpty(afterId))
                parameters.Add($"after={afterId}");

            string requestPath = $"/api/v5/trade/orders-history-archive";

            if (parameters.Count > 0)
                requestPath += $"?{string.Join("&", parameters)}";

            var responce = this.SendPrivateGetRequest<OKExOrder[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExOrder[0];
        }
        internal OKExAlgoOrder[] GetHistoryAlgoOrders(OKExSymbol okexSymbol, OKExInstrumentType? type, OKExAlgoOrderType orderType, OKExAlgoOrderState? state, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            string innerErrorMessage = string.Empty;

            var items = this.PaginationLoaderWithRange(
                (afterId) => this.GetHistoryAlgoOrders(okexSymbol, type, orderType, state, afterId, token, out innerErrorMessage),
                fromDateTime,
                toDateTime);

            error = innerErrorMessage;
            return items;
        }
        internal OKExAlgoOrder[] GetHistoryAlgoOrders(OKExAlgoOrderType orderType, OKExAlgoOrderState? state, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            return this.GetHistoryAlgoOrders(null, null, orderType, state, fromDateTime, toDateTime, token, out error);
        }
        private OKExAlgoOrder[] GetHistoryAlgoOrders(OKExSymbol okexSymbol, OKExInstrumentType? type, OKExAlgoOrderType orderType, OKExAlgoOrderState? state, string afterId, CancellationToken token, out string error)
        {
            OKExRateLimitManager.AlgoOrdersHistory.WaitMyTurn();

            var requestPath = $"/api/v5/trade/orders-algo-history";
            var parameters = new List<string>()
            {
                $"ordType={orderType.GetEnumMember()}"
            };

            if (okexSymbol != null)
                parameters.Add($"instId={okexSymbol.OKExInstrumentId}");

            if (state.HasValue)
                parameters.Add($"state={state.GetEnumMember()}");

            if (type.HasValue)
                parameters.Add($"instType={type.GetEnumMember()}");

            if (!string.IsNullOrEmpty(afterId))
                parameters.Add($"&after={afterId}");

            if (parameters.Count > 0)
                requestPath += $"?{string.Join("&", parameters)}";

            var responce = this.SendPrivateGetRequest<OKExAlgoOrder[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExAlgoOrder[0];
        }
        internal OKExDepositRecord[] GetDepositHistory(string currency, string afterId, CancellationToken token, out string error)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrEmpty(currency))
                parameters.Add($"ccy={currency}");

            if (!string.IsNullOrEmpty(afterId))
                parameters.Add($"after={afterId}");

            string requestPath = $"/api/v5/asset/deposit-history";

            if (parameters.Count > 0)
                requestPath += $"?{string.Join("&", parameters)}";

            var responce = this.SendPrivateGetRequest<OKExDepositRecord[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExDepositRecord[0];
        }
        internal OKExDepositRecord[] GetDepositHistory(DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            string innerErrorMessage = string.Empty;

            var items = this.PaginationLoaderWithRange(
                (afterId) => this.GetDepositHistory(string.Empty, afterId, token, out innerErrorMessage),
                fromDateTime,
                toDateTime);

            error = innerErrorMessage;
            return items;
        }
        internal OKExWithdrawalRecord[] GetWithdrawalHistory(string currency, string afterId, CancellationToken token, out string error)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrEmpty(currency))
                parameters.Add($"ccy={currency}");

            if (!string.IsNullOrEmpty(afterId))
                parameters.Add($"after={afterId}");

            string requestPath = $"/api/v5/asset/withdrawal-history";

            if (parameters.Count > 0)
                requestPath += $"?{string.Join("&", parameters)}";

            var responce = this.SendPrivateGetRequest<OKExWithdrawalRecord[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExWithdrawalRecord[0];
        }
        internal OKExWithdrawalRecord[] GetWithdrawalHistory(DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            string innerErrorMessage = string.Empty;

            var items = this.PaginationLoaderWithRange(
                (afterId) => this.GetWithdrawalHistory(string.Empty, afterId, token, out innerErrorMessage),
                fromDateTime,
                toDateTime);

            error = innerErrorMessage;
            return items;
        }
        internal OKExTransaction[] GetTransactions(OKExInstrumentType type, string afterId, CancellationToken token, out string error)
        {
            OKExRateLimitManager.TransactionDetails.WaitMyTurn();

            var parameters = new List<string>
            {
                $"instType={type.GetEnumMember()}"
            };

            if (!string.IsNullOrEmpty(afterId))
                parameters.Add($"after={afterId}");

            string requestPath = $"/api/v5/trade/fills-history";

            if (parameters.Count > 0)
                requestPath += $"?{string.Join("&", parameters)}";

            var responce = this.SendPrivateGetRequest<OKExTransaction[]>(this.settings.RestEndpoint, requestPath, token);
            error = responce.Message;
            return responce.Data ?? new OKExTransaction[0];
        }
        internal OKExTransaction[] GetTransactions(OKExInstrumentType type, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error)
        {
            string innerErrorMessage = string.Empty;

            var items = this.PaginationLoaderWithRange(
                (afterId) => this.GetTransactions(type, afterId, token, out innerErrorMessage),
                fromDateTime,
                toDateTime);

            error = innerErrorMessage;
            return items;
        }

        #endregion Data requests

        #region Trading operations

        internal OKExTradingOrderResponce PlaceOrder(OKExPlaceOrderRequest request, CancellationToken token, out string error)
        {
            var httpResponce = this.SendPrivatePostRequest<OKExTradingOrderResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/order", request, token);
            error = httpResponce.Message;
            return httpResponce.Data?.FirstOrDefault();
        }
        internal OKExPlaceAlgoOrderResponce PlaceAlgoOrder(OKExPlaceAlgoOrderRequest request, CancellationToken token, out string error)
        {
            var httpResponce = this.SendPrivatePostRequest<OKExPlaceAlgoOrderResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/order-algo", request, token);
            error = httpResponce.Message;
            return httpResponce.Data?.FirstOrDefault();
        }
        internal OKExTradingOrderResponce AmendOrder(OKExAmendOrderRequest request, CancellationToken token, out string error)
        {
            var httpResponce = this.SendPrivatePostRequest<OKExTradingOrderResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/amend-order", request, token);
            error = httpResponce.Message;
            return httpResponce.Data?.FirstOrDefault();
        }
        internal OKExTradingOrderResponce CancelOrder(OKExCancelOrderRequest request, CancellationToken token, out string error)
        {
            var httpResponce = this.SendPrivatePostRequest<OKExTradingOrderResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/cancel-order", request, token);
            error = httpResponce.Message;
            return httpResponce.Data?.FirstOrDefault();
        }
        internal OKExPlaceAlgoOrderResponce CancelAlgoOrders(OKExCancelAlgoOrderRequest[] requests, CancellationToken token, out string error)
        {
            if (requests.Length > 10)
                throw new ArgumentException("Too many cancel requests. Limit = 10");

            var httpResponce = this.SendPrivatePostRequest<OKExPlaceAlgoOrderResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/cancel-algos", requests, token);
            error = httpResponce.Message;
            return httpResponce.Data?.FirstOrDefault();
        }
        internal OKExPlaceAlgoOrderResponce CancelAlgoOrder(OKExCancelAlgoOrderRequest request, CancellationToken token, out string error)
        {
            return this.CancelAlgoOrders(new OKExCancelAlgoOrderRequest[] { request }, token, out error);
        }
        internal OKExClosePositionResponce ClosePosition(OKExClosePositionRequest request, CancellationToken token, out string error)
        {
            var httpResponce = this.SendPrivatePostRequest<OKExClosePositionResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/close-position", request, token);
            error = httpResponce.Message;
            return httpResponce.Data?.FirstOrDefault();
        }

        #endregion Trading operations

        #region Subscriptions

        private bool SendAuthWebsocketRequest(CancellationToken token)
        {
            // generate sign
            string timestamp = (Core.Instance.TimeUtils.DateTimeUtcNow.ToUnixSeconds()).ToString();
            string sign = OKExSignGenerator.Generate(timestamp, HttpMethod.Get, "/users/self/verify", this.secret);

            // send request
            this.privateWebsocket.SendRequest(new OKExLoginRequest()
            {
                Args = new OKExLoginArguments[]
                {
                    new OKExLoginArguments()
                    {
                        ApiKey = this.apiKey,
                        Passphrase = this.passPhrase,
                        Timestamp = timestamp,
                        Sigh = sign
                    }
                }
            });

            // wait 'login' responce
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!token.IsCancellationRequested && !cts.IsCancellationRequested && !this.IsLogged && this.LastErrorMessage.Length == 0)
                Thread.Sleep(100);
            cts.Dispose();

            return this.IsLogged;
        }
        private bool SubscribeToTradingChannels(CancellationToken token)
        {
            // send request
            this.privateWebsocket.SendRequest(new OKExSubscribeRequest()
            {
                Args = new OKExChannelRequest[]
                {
                    new OKExChannelRequest() { ChannelName = OKExChannels.ACCOUNT },
                    new OKExChannelRequest() { ChannelName = OKExChannels.ORDERS,      InstrumentType = OKExInstrumentType.Any },
                    new OKExChannelRequest() { ChannelName = OKExChannels.ALGO_ORDERS, InstrumentType = OKExInstrumentType.Any },
                    new OKExChannelRequest() { ChannelName = OKExChannels.POSITIONS,   InstrumentType = OKExInstrumentType.Any },
                }
            });

            // wait 'login' responce
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!token.IsCancellationRequested && !cts.IsCancellationRequested && (!this.subscribedTradingChannels.Contains(OKExChannels.ACCOUNT) || !this.subscribedTradingChannels.Contains(OKExChannels.ORDERS) || !this.subscribedTradingChannels.Contains(OKExChannels.ALGO_ORDERS) || !this.subscribedTradingChannels.Contains(OKExChannels.POSITIONS)))
                Thread.Sleep(100);
            cts.Dispose();

            return !cts.IsCancellationRequested;
        }

        #endregion 

        #region Event handlers

        private void PrivateWebsocket_OnResponceReceive(JObject jObject)
        {
            // 
            switch (jObject.SelectToken("event")?.ToString())
            {
                case OKExConsts.LOGIN:
                    {
                        if (jObject.SelectToken("code")?.ToString() == "0")
                            this.IsLogged = true;
                        break;
                    }
                case OKExConsts.SUBSCRIBE:
                    {
                        if (jObject?.SelectToken("arg.channel")?.ToString() is string channelName)
                            this.subscribedTradingChannels.Add(channelName);
                        break;
                    }
                case OKExConsts.UNSUBSCRIBE:
                    {
                        if (jObject?.SelectToken("arg.channel")?.ToString() is string channelName)
                            this.subscribedTradingChannels.Remove(channelName);
                        break;
                    }
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

            //
            if (jObject.SelectToken("data") is JToken data)
            {
                switch (jObject?.SelectToken("arg.channel")?.ToString())
                {
                    case OKExChannels.ACCOUNT:
                        {
                            if (data.ToObject<OKExBalance[]>()?.FirstOrDefault() is OKExBalance oKExBalance)
                                this.OnBalanceChanged?.Invoke(oKExBalance);
                            break;
                        }
                    case OKExChannels.ORDERS:
                        {
                            var orders = data.ToObject<OKExOrder[]>();
                            foreach (var o in orders)
                                this.OnOrder?.Invoke(o);
                            break;
                        }
                    case OKExChannels.ALGO_ORDERS:
                        {
                            var orders = data.ToObject<OKExAlgoOrder[]>();
                            foreach (var o in orders)
                                this.OnAlgoOrder?.Invoke(o);
                            break;
                        }
                    case OKExChannels.POSITIONS:
                        {
                            var positions = data.ToObject<OKExPosition[]>();
                            foreach (var p in positions)
                                this.OnPosition?.Invoke(p);
                            break;
                        }
                }
            }
        }

        #endregion Event handlers

        #region Misc

        private OKExRestResponce<T> SendPrivateGetRequest<T>(string host, string endpoint, CancellationToken token)
        {
            try
            {
                lock (this.sendPrivateRequestLockKey)
                {
                    string timestamp = Core.Instance.TimeUtils.DateTimeUtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    string sign = OKExSignGenerator.Generate(timestamp, HttpMethod.Get, endpoint, this.secret);

                    this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("OK-ACCESS-TIMESTAMP", timestamp);
                    this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("OK-ACCESS-SIGN", sign);
                }

                var httpResponce = this.httpClient.GetAsync($"{host}{endpoint}", HttpCompletionOption.ResponseHeadersRead, token)?.Result;

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
        private OKExRestResponce<T> SendPrivatePostRequest<T>(string host, string endpoint, object body, CancellationToken token)
        {
            try
            {
                string json = JsonConvert.SerializeObject(body, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                lock (this.sendPrivateRequestLockKey)
                {
                    string timestamp = Core.Instance.TimeUtils.DateTimeUtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    string sign = OKExSignGenerator.Generate(timestamp, HttpMethod.Post, endpoint + json, this.secret);

                    this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("OK-ACCESS-TIMESTAMP", timestamp);
                    this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("OK-ACCESS-SIGN", sign);
                }

                var httpResponce = this.httpClient.PostAsync($"{host}{endpoint}", new StringContent(json, Encoding.UTF8, "application/json"), token)?.Result;

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

        private T[] PaginationLoader<T>(Func<string, T[]> loadingFunction, int limit = 100) where T : IPaginationLoadingItem
        {
            return this.PaginationLoaderWithRange(loadingFunction, DateTime.MinValue, DateTime.MaxValue, limit);
        }
        private T[] PaginationLoaderWithRange<T>(Func<string, T[]> loadingFunction, DateTime from, DateTime to, int limit = 100) where T : IPaginationLoadingItem
        {
            string afterId = null;

            var itemsCache = new List<T>();

            var keepGoing = true;
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
                        afterId = items[items.Length - 1].AfterId;
                    else
                        afterId = items[0].AfterId;
                }
                else
                    keepGoing = false;
            }

            return itemsCache.ToArray();
        }

        protected override bool TryGetChannelName(OKExSymbol symbol, OKExSubscriptionType subscriptionType, out string channelName)
        {
            switch (subscriptionType)
            {
                case OKExSubscriptionType.Level2 when this.feeRatesCache.TryGetValue(symbol.InstrumentType, out var feeRate) && feeRate.IsVIP5orGreater:
                    {
                        channelName = OKExChannels.ORDER_BOOK_400_TBT;
                        break;
                    }
                default:
                    return base.TryGetChannelName(symbol, subscriptionType, out channelName);
            }

            return channelName != null;
        }

        #endregion Misc
    }
}
