// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using Bitfinex.API.Abstractions;
using Bitfinex.API.JsonConverters;
using Bitfinex.API.Misc;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API;

internal class BitfinexPrivateWebSocketApi : BitfinexWebSocketApi, IBitfinexPrivateWebSocketApi
{
    private readonly BitfinexAuthHelper authHelper;
    private static readonly JsonSerializerSettings jsonSettings;

    static BitfinexPrivateWebSocketApi()
    {
        jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new BitfinexWalletJsonConverter(),
                new BitfinexOrderJsonConverter(),
                new BitfinexPositionJsonConverter(),
                new BitfinexUserTradeJsonConverter(),
                new BitfinexNotificationJsonConverter()
            }
        };
    }

    public BitfinexPrivateWebSocketApi(string endpoint, BitfinexAuthHelper authHelper)
        : base(endpoint)
    {
        this.authHelper = authHelper;
    }

    public int? Authenticate(string apiKey, string apiSecret, out string error, CancellationToken cancellation)
    {
        error = default;

        long nonce = this.authHelper.GenerateNonce();
        string payload = BitfinexAuthHelper.GetPayload(nonce);
        string signature = BitfinexAuthHelper.ComputeSignature(payload, apiSecret);

        var message = new BitfinexAuthMessage
        {
            ApiKey = apiKey,
            Signature = signature,
            Nonce = nonce,
            Payload = payload
        };

        var response = this.SendMessage(message, cancellation);
        if (!string.IsNullOrEmpty(response.Message))
        {
            error = response.FormatError();
            return null;
        }

        return response.UserId;
    }

    private protected override void DeserializeData(JArray message)
    {
        string eventType = message[1].Value<string>();

        var eventArgs = eventType switch
        {
            BitfinexEvent.WALLET_UPDATE => this.HandleWalletUpdate(message),
            BitfinexEvent.ORDER_NEW or BitfinexEvent.ORDER_UPDATE or BitfinexEvent.ORDER_CANCEL => this.HandleOrderUpdate(message),
            BitfinexEvent.POSITION_NEW or BitfinexEvent.POSITION_UPDATE or BitfinexEvent.POSITION_CLOSE => this.HandlePositionUpdate(message),
            //BitfinexEvent.TRADE_EXECUTED => this.HandleUserTradeUpdate(message),
            BitfinexEvent.TRADE_EXECUTION_UPDATED => this.HandleUserTradeUpdate(message),
            BitfinexEvent.NOTIFICATION => this.HandleNotification(message),
            _ => null
        };

        if (eventArgs == null)
            return;

        eventArgs.Event = eventType;

        this.OnNewData(eventArgs);
    }

    #region Handlers

    private BitfinexEventArgs HandleWalletUpdate(JArray message)
    {
        var data = message[2].Value<JArray>();
        if (data == null)
            return null;

        var wallet = JsonConvert.DeserializeObject<BitfinexWallet>(data.ToString(), jsonSettings);

        return new BitfinexEventArgs
        {
            WalletUpdate = wallet
        };
    }

    private BitfinexEventArgs HandleOrderUpdate(JArray message)
    {
        var data = message[2].Value<JArray>();
        if (data == null)
            return null;

        var order = JsonConvert.DeserializeObject<BitfinexOrder>(data.ToString(), jsonSettings);

        return new BitfinexEventArgs
        {
            OrderUpdate = order
        };
    }

    private BitfinexEventArgs HandlePositionUpdate(JArray message)
    {
        var data = message[2].Value<JArray>();
        if (data == null)
            return null;

        var position = JsonConvert.DeserializeObject<BitfinexPosition>(data.ToString(), jsonSettings);

        return new BitfinexEventArgs
        {
            PositionUpdate = position
        };
    }

    private BitfinexEventArgs HandleUserTradeUpdate(JArray message)
    {
        var data = message[2].Value<JArray>();
        if (data == null)
            return null;

        var trade = JsonConvert.DeserializeObject<BitfinexUserTrade>(data.ToString(), jsonSettings);

        return new BitfinexEventArgs
        {
            UserTrade = trade
        };
    }

    private BitfinexEventArgs HandleNotification(JArray message)
    {
        var data = message[2].Value<JArray>();
        if (data == null)
            return null;

        var notification = JsonConvert.DeserializeObject<BitfinexNotification>(data.ToString(), jsonSettings);

        return new BitfinexEventArgs
        {
            Notification = notification
        };
    }

    #endregion Handlers
}