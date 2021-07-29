using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API.REST;
using OKExV5Vendor.API.Websocket.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;
using WebSocket4Net;

namespace OKExV5Vendor.API.Websocket
{
    class OKExWebSocket
    {
        private const int MAX_SUB_UNSUB_CHANNELS_PER_REQUEST = 60;

        protected readonly WebSocket webSocket;
        protected readonly ActionBufferedProcessor inputProcessor;

        private readonly bool useQueueRequest;
        private readonly object subscriptionLockKey;
        private readonly Timer innerTimer;
        private readonly Timer pingPongTimer;
        private readonly IDictionary<string, IDictionary<string, OKExChannelRequest>> pendingSubscribeChannels;
        private readonly IDictionary<string, IDictionary<string, OKExChannelRequest>> pendingUnsubsribeChannels;
        private readonly Stopwatch sw;

        public event Action<JObject> OnResponceReceive;

        public WebSocketState State => this.webSocket.State;
        public TimeSpan RoundTripTime => this.sw.Elapsed;

        public OKExWebSocket(string uri, bool useQueueRequest = false)
        {
            this.webSocket = new WebSocket(uri, sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls)
            {
                EnableAutoSendPing = false
            };
            
            this.useQueueRequest = useQueueRequest;

            this.webSocket.Opened += this.WebSocket_Opened;
            this.webSocket.MessageReceived += this.WebSocket_MessageReceived;
            this.webSocket.Error += this.WebSocket_Error;
            this.webSocket.Closed += this.WebSocket_Closed;

            this.innerTimer = new Timer(this.SubscriptionTimerHandler, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            this.pingPongTimer = new Timer(this.PingPongTimerHandler, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            this.sw = new Stopwatch();

            this.subscriptionLockKey = new object();
            this.inputProcessor = new ActionBufferedProcessor();
            this.pendingSubscribeChannels = new Dictionary<string, IDictionary<string, OKExChannelRequest>>();
            this.pendingUnsubsribeChannels = new Dictionary<string, IDictionary<string, OKExChannelRequest>>();
        }

        internal bool Connect(CancellationToken token, out string error)
        {
            error = null;

            this.webSocket.Open();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            while (!token.IsCancellationRequested && !cts.IsCancellationRequested && this.webSocket.State != WebSocketState.Open)
                Thread.Sleep(100);

            if (cts.IsCancellationRequested)
                error = "Time out";

            cts.Dispose();

            var isConnected = this.webSocket.State == WebSocketState.Open;

            if (isConnected)
            {
                this.inputProcessor.Start();

                if (this.useQueueRequest)
                    this.innerTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

                this.pingPongTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(25));
            }
            
            return isConnected;
        }
        internal void Disconnect()
        {
            this.inputProcessor?.Stop();
            this.innerTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            this.pingPongTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (this.webSocket != null)
            {
                this.webSocket.Close();
                this.webSocket.Opened -= this.WebSocket_Opened;
                this.webSocket.MessageReceived -= this.WebSocket_MessageReceived;
                this.webSocket.Error -= this.WebSocket_Error;
                this.webSocket.Closed -= this.WebSocket_Closed;
            }
        }

        private void WebSocket_Closed(object sender, EventArgs e)
        {
            //Core.Instance.Loggers.Log("OKEx: websocket was closed");
        }
        private void WebSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            //Core.Instance.Loggers.Log("OKEx: websocket throw " + e.Exception.Message);
        }
        private void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                if (this.sw.IsRunning && e.Message == "pong")
                    this.sw.Stop();
                else
                    this.OnResponceReceive?.Invoke(JObject.Parse(e.Message));
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex, null, LoggingLevel.Verbose, OKExConsts.VENDOR_NAME);
            }
        }
        private void WebSocket_Opened(object sender, EventArgs e)
        {
            //Core.Instance.Loggers.Log("OKEx: websocket was opened");
        }

        internal void SendRequest(string request) => this.webSocket.Send(request);

        internal void AddRequestToQueue(params OKExChannelRequest[] items)
        {
            lock (this.subscriptionLockKey)
            {
                foreach (var item in items)
                {
                    if (this.pendingUnsubsribeChannels.TryGetValue(item.InstrumentId, out var unsubChannels) && unsubChannels.Remove(item.ChannelName))
                    {
                        if (unsubChannels.Count == 0)
                            this.pendingUnsubsribeChannels.Remove(item.InstrumentId);
                    }
                    else
                    {
                        if (!this.pendingSubscribeChannels.TryGetValue(item.InstrumentId, out var subChannels))
                            this.pendingSubscribeChannels[item.InstrumentId] = subChannels = new Dictionary<string, OKExChannelRequest>();

                        if (!subChannels.ContainsKey(item.ChannelName))
                            subChannels[item.ChannelName] = item;
                    }
                }
            }
        }
        internal void RemoveFromQueue(params OKExChannelRequest[] items)
        {
            lock (this.subscriptionLockKey)
            {
                foreach (var item in items)
                {
                    if (this.pendingSubscribeChannels.TryGetValue(item.InstrumentId, out var subChannels) && subChannels.Remove(item.ChannelName))
                    {
                        if (subChannels.Count == 0)
                            this.pendingSubscribeChannels.Remove(item.InstrumentId);
                    }
                    else
                    {
                        if (!this.pendingUnsubsribeChannels.TryGetValue(item.InstrumentId, out var unsubChannels))
                            this.pendingUnsubsribeChannels[item.InstrumentId] = unsubChannels = new Dictionary<string, OKExChannelRequest>();

                        if (!unsubChannels.ContainsKey(item.ChannelName))
                            unsubChannels[item.ChannelName] = item;
                    }
                }
            }
        }
        private void SubscriptionTimerHandler(object state)
        {
            lock (this.subscriptionLockKey)
            {
                if (this.pendingUnsubsribeChannels.Count > 0)
                {
                    var unsubChannels = this.pendingUnsubsribeChannels.Values.SelectMany(s => s.Values).ToArray();
                    var requestChannels = new List<OKExChannelRequest>();

                    for (int i = 0; i < Math.Min(MAX_SUB_UNSUB_CHANNELS_PER_REQUEST, unsubChannels.Length); i++)
                    {
                        var ch = unsubChannels[i];
                        requestChannels.Add(ch);

                        this.pendingUnsubsribeChannels[ch.InstrumentId].Remove(ch.ChannelName);
                        if (this.pendingUnsubsribeChannels[ch.InstrumentId].Count == 0)
                            this.pendingUnsubsribeChannels.Remove(ch.InstrumentId);
                    }

                    if (requestChannels.Count > 0)
                        this.webSocket.Send(JsonConvert.SerializeObject(new OKExUnsubscribeRequest() { Args = requestChannels.ToArray() }));
                }

                if (this.pendingSubscribeChannels.Count > 0)
                {
                    var subChannels = this.pendingSubscribeChannels.Values.SelectMany(s => s.Values).ToArray();
                    var requestChannels = new List<OKExChannelRequest>();

                    for (int i = 0; i < Math.Min(MAX_SUB_UNSUB_CHANNELS_PER_REQUEST, subChannels.Length); i++)
                    {
                        var ch = subChannels[i];
                        requestChannels.Add(ch);

                        this.pendingSubscribeChannels[ch.InstrumentId].Remove(ch.ChannelName);
                        if (this.pendingSubscribeChannels[ch.InstrumentId].Count == 0)
                            this.pendingSubscribeChannels.Remove(ch.InstrumentId);
                    }

                    if (requestChannels.Count > 0)
                        this.webSocket.Send(JsonConvert.SerializeObject(new OKExSubscribeRequest() { Args = requestChannels.ToArray() }));
                }
            }
        }
        private void PingPongTimerHandler(object state)
        {
            this.sw.Restart();
            this.webSocket.Send("ping");
        }
    }
}
