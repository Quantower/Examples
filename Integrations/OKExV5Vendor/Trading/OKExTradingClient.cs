// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGettext.Plural.Ast;
using OKExV5Vendor.API;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.RateLimit;
using OKExV5Vendor.API.REST;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket;
using OKExV5Vendor.API.Websocket.Models;
using OKExV5Vendor.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.Trading;

internal sealed class OKExTradingClient : OKExMarketClient
{
    #region Parameters

    private readonly string secret;
    private readonly string apiKey;
    private readonly string passPhrase;

    private readonly HttpClient httpClient;
    private readonly OKExWebSocket privateWebsocket;
    private readonly OKExWebSocket privateBusinessWebsocket;

    private readonly IDictionary<OKExInstrumentType, OKExFeeRate> feeRatesCache;
    private readonly HashSet<string> subscribedTradingChannels;

    protected OKExTradingRateLimitManager TradingRateLimitManager
    {
        get => this.tradingRateLimitManager ?? OKExRateLimitManager.Trading;
        set => this.tradingRateLimitManager = value;
    }
    private OKExTradingRateLimitManager tradingRateLimitManager;

    internal bool IsLogged { get; private set; }
    internal bool IsDemoMode => this.settings.IsDemo;
    internal override bool IsConnected => base.IsConnected && this.privateWebsocket.State == WebSocket4Net.WebSocketState.Open && this.privateBusinessWebsocket.State == WebSocket4Net.WebSocketState.Open;

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

        this.privateWebsocket = new OKExWebSocket(this.settings.PrivateWebsoketEndpoint, useQueueRequest: false);
        this.privateWebsocket.OnResponceReceive += this.PrivateWebsocket_OnResponceReceive;

        this.privateBusinessWebsocket = new OKExWebSocket(this.settings.PrivateBusinessWss, useQueueRequest: false);
        this.privateBusinessWebsocket.OnResponceReceive += this.PrivateWebsocket_OnResponceReceive;

        this.httpClient = new HttpClient();
        this.httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", apiKey);
        this.httpClient.DefaultRequestHeaders.Add("OK-ACCESS-PASSPHRASE", passPhrase);
    }

    #region Connect

    internal override bool Connect(CancellationToken token, out string error)
    {
        bool isConnected = base.Connect(token, out error) &&
                          this.privateWebsocket.Connect(token, out error) &&
                          this.SendAuthWebsocketRequest(this.privateWebsocket, token) &&

                          this.privateBusinessWebsocket.Connect(token, out error) &&        
                          this.SendAuthWebsocketRequest(this.privateBusinessWebsocket, token) &&

                          this.SubscribeToTradingChannels(token);

        if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(this.LastErrorMessage))
            error = this.LastErrorMessage;

        if (isConnected)
        {
            var instTypes = new OKExInstrumentType[] { OKExInstrumentType.Spot, OKExInstrumentType.Margin, OKExInstrumentType.Futures, OKExInstrumentType.Option, OKExInstrumentType.Swap };

            foreach (var item in instTypes)
            {
                var feeRate = this.GetFeeRate(item, token, out error);

                if (token.IsCancellationRequested || !string.IsNullOrEmpty(error))
                    break;

                if (feeRate != null)
                    this.feeRatesCache[item] = feeRate;
            }
        }

        return isConnected && string.IsNullOrEmpty(error);
    }
    internal override void Disconnect()
    {
        this.privateWebsocket.Disconnect();
        this.privateBusinessWebsocket.Disconnect();
        base.Disconnect();
    }

    #endregion Connect

    #region Data requests

    internal OKExLeverage[] GetLeverages(OKExSymbol[] leverageBasedSymbols, OKExMarginMode mode, CancellationToken token, out string error)
    {
        error = null;
        var result = new List<OKExLeverage>();

        var splittedSymbols = leverageBasedSymbols.Select(s => s.OKExInstrumentId).ToList().SplitList(20);

        foreach (var cache in splittedSymbols)
        {
            this.TradingRateLimitManager.GetLeverage.WaitMyTurn(token);

            var parameters = new List<string>
            {
                $"instId={string.Join(',', cache)}",
                $"mgnMode={mode.GetEnumMember()}"
            };

            var responce = this.SendPrivateGetRequest<OKExLeverage[]>(this.settings.RestEndpoint, $"/api/v5/account/leverage-info?{string.Join('&', parameters)}", token);
            error = responce.Message;

            if (!string.IsNullOrEmpty(error))
                break;

            if (responce.Data != null)
                result.AddRange(responce.Data);
        }

        return result.ToArray();
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
        this.TradingRateLimitManager.GetOrdersList.WaitMyTurn(token);

        string requestPath = $"/api/v5/trade/orders-pending";

        if (!string.IsNullOrEmpty(afterAlgoId))
            requestPath += $"?after={afterAlgoId}";

        var responce = this.SendPrivateGetRequest<OKExOrder[]>(this.settings.RestEndpoint, requestPath, token);
        error = responce.Message;
        return responce.Data ?? Array.Empty<OKExOrder>();
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
        string innerErrorMessage = string.Empty;

        var items = this.PaginationLoader((afterId) => this.GetAlgoOrdersList(orderType, afterId, token, out innerErrorMessage));
        error = innerErrorMessage;

        return items;
    }
    internal OKExAlgoOrder[] GetAlgoOrdersList(OKExAlgoOrderType orderType, string afterAlgoId, CancellationToken token, out string error)
    {
        this.TradingRateLimitManager.GetAlgoOrdersList.WaitMyTurn(token);

        string requestPath = $"/api/v5/trade/orders-algo-pending?ordType={orderType.GetEnumMember()}";

        if (!string.IsNullOrEmpty(afterAlgoId))
            requestPath += $"&after={afterAlgoId}";

        var responce = this.SendPrivateGetRequest<OKExAlgoOrder[]>(this.settings.RestEndpoint, requestPath, token);
        error = responce.Message;
        return responce.Data ?? Array.Empty<OKExAlgoOrder>();
    }
    internal OKExPosition[] GetPositions(CancellationToken token, out string error)
    {
        this.TradingRateLimitManager.GetPositions.WaitMyTurn(token);

        var responce = this.SendPrivateGetRequest<OKExPosition[]>(this.settings.RestEndpoint, "/api/v5/account/positions", token);
        error = responce.Message;
        return responce.Data ?? Array.Empty<OKExPosition>();
    }
    internal OKExAccount GetAccount(CancellationToken token, out string error)
    {
        this.TradingRateLimitManager.GetAccount.WaitMyTurn(token);

        var responce = this.SendPrivateGetRequest<OKExAccount[]>(this.settings.RestEndpoint, "/api/v5/account/config", token);
        error = responce.Message;
        return responce.Data?.FirstOrDefault();
    }
    internal OKExFeeRate GetFeeRate(OKExInstrumentType type, CancellationToken token, out string error)
    {
        this.TradingRateLimitManager.GetFeeRate.WaitMyTurn(token);

        var responce = this.SendPrivateGetRequest<OKExFeeRate[]>(this.settings.RestEndpoint, $"/api/v5/account/trade-fee?instType={type.GetEnumMember()}", token);
        error = responce.Message;
        return responce.Data?.FirstOrDefault();
    }
    internal OKExOrder[] GetHistoryOrders(OKExSymbol okexSymbol, DateTime fromDateTime, DateTime toDateTime, OKExOrderState? state, CancellationToken token, out string error) => this.GetHistoryOrders(okexSymbol, okexSymbol.InstrumentType, state, fromDateTime, toDateTime, token, out error);
    internal OKExOrder[] GetHistoryOrders(OKExInstrumentType type, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error) => this.GetHistoryOrders(null, type, null, fromDateTime, toDateTime, token, out error);
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
        this.TradingRateLimitManager.OrdersHistory.WaitMyTurn(token);

        if (type == OKExInstrumentType.Index || type == OKExInstrumentType.Any)
        {
            error = "Unsupported instrument type";
            return Array.Empty<OKExOrder>();
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
        return responce.Data ?? Array.Empty<OKExOrder>();
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
    internal OKExAlgoOrder[] GetHistoryAlgoOrders(OKExAlgoOrderType orderType, OKExAlgoOrderState? state, DateTime fromDateTime, DateTime toDateTime, CancellationToken token, out string error) => this.GetHistoryAlgoOrders(null, null, orderType, state, fromDateTime, toDateTime, token, out error);
    private OKExAlgoOrder[] GetHistoryAlgoOrders(OKExSymbol okexSymbol, OKExInstrumentType? type, OKExAlgoOrderType orderType, OKExAlgoOrderState? state, string afterId, CancellationToken token, out string error)
    {
        this.TradingRateLimitManager.AlgoOrdersHistory.WaitMyTurn(token);

        string requestPath = $"/api/v5/trade/orders-algo-history";
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
        return responce.Data ?? Array.Empty<OKExAlgoOrder>();
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
        return responce.Data ?? Array.Empty<OKExDepositRecord>();
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
        return responce.Data ?? Array.Empty<OKExWithdrawalRecord>();
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
        this.TradingRateLimitManager.TransactionDetails.WaitMyTurn(token);

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
        return responce.Data ?? Array.Empty<OKExTransaction>();
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
    internal OKExPlaceAlgoOrderResponce CancelAlgoOrder(OKExCancelAlgoOrderRequest request, CancellationToken token, out string error) => this.CancelAlgoOrders(new OKExCancelAlgoOrderRequest[] { request }, token, out error);
    internal OKExClosePositionResponce ClosePosition(OKExClosePositionRequest request, CancellationToken token, out string error)
    {
        var httpResponce = this.SendPrivatePostRequest<OKExClosePositionResponce[]>(this.settings.RestEndpoint, "/api/v5/trade/close-position", request, token);
        error = httpResponce.Message;
        return httpResponce.Data?.FirstOrDefault();
    }

    internal OKExLeverage SetLeverage(OKExSetLeverageRequest request, CancellationToken token, out string error)
    {
        this.TradingRateLimitManager.SetLeverage.WaitMyTurn(token);

        var httpResponce = this.SendPrivatePostRequest<OKExLeverage[]>(this.settings.RestEndpoint, "/api/v5/account/set-leverage", request, token);
        error = httpResponce.Message;
        return httpResponce.Data?.FirstOrDefault();
    }

    #endregion Trading operations

    #region Subscriptions

    private bool SendAuthWebsocketRequest(OKExWebSocket webSocket, CancellationToken token)
    {
        // generate sign
        string timestamp = Core.Instance.TimeUtils.DateTimeUtcNow.ToUnixSeconds().ToString();
        string sign = OKExSignGenerator.Generate(timestamp, HttpMethod.Get, "/users/self/verify", this.secret);

        this.IsLogged = false;
        this.LastErrorMessage = string.Empty;

        // send request
        webSocket.SendRequest(new OKExLoginRequest()
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
                new OKExChannelRequest() { ChannelName = OKExChannels.POSITIONS,   InstrumentType = OKExInstrumentType.Any },
            }
        });

        this.privateBusinessWebsocket.SendRequest(new OKExSubscribeRequest()
        {
            Args = new OKExChannelRequest[]
            {
                new OKExChannelRequest() { ChannelName = OKExChannels.ALGO_ORDERS, InstrumentType = OKExInstrumentType.Any },
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

    private OKExRestResponce<TResponse> SendPrivateGetRequest<TResponse>(string host, string endpoint, CancellationToken token, bool allowToResend = true, int tryCount = 0)
    {
        var response = this.SendRestRequest<TResponse>(() =>
        {
            using var client = this.CreatePrivateHttpClient(endpoint, HttpMethod.Get);
            return client.GetAsync($"{host}{endpoint}", HttpCompletionOption.ResponseHeadersRead, token)?.Result;
        }, token);

        //
        if (response.Code == REQUESTS_TOO_FREQUENT_CODE && allowToResend && ++tryCount < MAX_SEND_REQUEST_TRYING_COUNT)
            return this.SendPrivateGetRequest<TResponse>(host, endpoint, token, allowToResend, tryCount);
        //
        else
            return response;
    }
    private OKExRestResponce<TResponse> SendPrivatePostRequest<TResponse>(string host, string endpoint, object body, CancellationToken token) => this.SendRestRequest<TResponse>(() =>
    {
        string json = JsonConvert.SerializeObject(body, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        using var client = this.CreatePrivateHttpClient(endpoint + json, HttpMethod.Post);
        return client.PostAsync($"{host}{endpoint}", new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json), token)?.Result;
    }, token);

    private HttpClient CreatePrivateHttpClient(string message, HttpMethod method)
    {
        var client = new HttpClient();

        string timestamp = Core.Instance.TimeUtils.DateTimeUtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string sign = OKExSignGenerator.Generate(timestamp, method, message, this.secret);

        client.DefaultRequestHeaders.Add("OK-ACCESS-KEY", this.apiKey);
        client.DefaultRequestHeaders.Add("OK-ACCESS-PASSPHRASE", this.passPhrase);
        client.DefaultRequestHeaders.Add("OK-ACCESS-TIMESTAMP", timestamp);
        client.DefaultRequestHeaders.Add("OK-ACCESS-SIGN", sign);

        if (this.settings.IsDemo)
            SetSimulatedTradingHeader(client);

        return client;
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

    internal void SetTradingRateLimitManager(string userId)
    {
        this.TradingRateLimitManager = OKExRateLimitManager.GetOrCreateTradingRateLimitManager(userId);
    }

    #endregion Misc
}