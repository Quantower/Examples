// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Bitfinex.API.Abstractions;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using System;
using System.Threading;
using WebSocket4Net;

namespace Bitfinex.API;

internal abstract class BitfinexWebSocketApi : IBitfinexWebSocketApi
{
    #region Properties

    public bool IsOpened => this.webSocket is { State: WebSocketState.Open };

    public event EventHandler<BitfinexEventArgs> NewData;
    public event EventHandler<BitfinexErrorEventArgs> Error;

    private readonly WebSocket webSocket;
    private ManualResetEventSlim connectEvent;

    private static readonly JsonSerializerSettings jsonSettings;

    private BitfinexMessage requestMessage;
    private BitfinexMessage responseMessage;
    private ManualResetEventSlim responseEvent;
    private readonly object sendLocker;

    private Exception lastError;
    private readonly TimeSpan timeout;

    #endregion Properties

    static BitfinexWebSocketApi()
    {
        jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    protected BitfinexWebSocketApi(string endpoint)
    {
        this.webSocket = new WebSocket(endpoint);
        this.webSocket.Opened += this.WebSocketOnOpened;
        this.webSocket.MessageReceived += this.WebSocketOnMessageReceived;
        this.webSocket.Error += this.WebSocketOnError;
        this.webSocket.Closed += this.WebSocketOnClosed;

        this.sendLocker = new object();

        this.timeout = TimeSpan.FromSeconds(30);
    }

    public void Connect(CancellationToken cancellation)
    {
        this.connectEvent = new ManualResetEventSlim();

        this.webSocket.OpenAsync();

        this.Wait(this.connectEvent, cancellation);

        if (this.webSocket.State != WebSocketState.Open && this.lastError != null)
            throw this.lastError;
    }

    public void Disconnect()
    {
        this.webSocket.Close("disconnect");
    }

    public void Ping(CancellationToken cancellation)
    {
        var message = new BitfinexMessage
        {
            Event = BitfinexEvent.PING
        };

        this.SendMessage(message, cancellation);
    }

    #region Event handlers

    private void WebSocketOnOpened(object sender, EventArgs e)
    {
        this.connectEvent.Set();
    }

    private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        DebugLog($"<<<<< {e.Message}");

        try
        {
            var jToken = JToken.Parse(e.Message);

            switch (jToken)
            {
                case JObject jObject:
                    var message = jObject.ToObject<BitfinexMessage>();

                    if (message == null)
                        return;

                    if (this.responseEvent != null && message.Event != BitfinexEvent.INFO)
                    {
                        this.responseMessage = message;
                        this.responseEvent?.Set();
                    }

                    break;
                case JArray jArray:
                    this.DeserializeData(jArray);
                    break;
            }
        }
        catch (Exception ex)
        {
            this.OnError(new BitfinexErrorEventArgs { Exception = ex });
        }
    }

    private void WebSocketOnError(object sender, ErrorEventArgs e)
    {
        this.lastError = e.Exception;

        this.connectEvent.Set();

        this.OnError(new BitfinexErrorEventArgs { Exception = e.Exception });
    }

    private void WebSocketOnClosed(object sender, EventArgs e)
    {
        this.connectEvent.Set();
    }

    #endregion Event handlers

    #region Misc

    private protected BitfinexMessage SendMessage(BitfinexMessage message, CancellationToken cancellation)
    {
        lock (this.sendLocker)
        {
            try
            {
                this.requestMessage = message;

                string json = JsonConvert.SerializeObject(message, jsonSettings);

                this.responseEvent = new();

                this.SendText(json);

                this.Wait(this.responseEvent, cancellation);

                return this.responseMessage;
            }
            finally
            {
                this.requestMessage = null;
                this.responseEvent = null;
            }
        }
    }

    private void SendText(string text)
    {
        this.webSocket.Send(text);

        DebugLog($">>>>> {text}");
    }

    private protected abstract void DeserializeData(JArray message);

    private void Wait(ManualResetEventSlim eventSlim, CancellationToken cancellation)
    {
        int waitResult = WaitHandle.WaitAny(new[]
        {
            eventSlim.WaitHandle,
            cancellation.WaitHandle,
            new CancellationTokenSource(this.timeout).Token.WaitHandle
        });

        if (waitResult == 2)
            throw new TimeoutException();
    }

    private protected void OnNewData(BitfinexEventArgs eventArgs) => this.NewData?.Invoke(this, eventArgs);

    private void OnError(BitfinexErrorEventArgs eventArgs) => this.Error?.Invoke(this, eventArgs);

    private static void DebugLog(string text)
    {
        System.Diagnostics.Debug.WriteLine(text);
    }

    #endregion Misc
}