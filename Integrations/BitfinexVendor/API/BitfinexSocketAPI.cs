// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using BitfinexVendor.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using WebSocket4Net;

namespace BitfinexVendor.API
{
    class BitfinexSocketApi
    {
        #region Properties
        public event EventHandler<BitfinexEventArgs> NewData;

        public BitfinexConnectionState ConnectionState
        {
            get => this.connectionState;
            private set
            {
                if (this.connectionState == value)
                    return;

                this.connectionState = value;
            }
        }
        private BitfinexConnectionState connectionState;

        private readonly WebSocket socket;

        private readonly Dictionary<string, Dictionary<BitfinexChannelType, int>> channelsCache;
        private readonly Dictionary<int, BitfinexChannelType> channelTypeCache;
        private readonly Dictionary<int, string> symbolByChannelCache;
        #endregion Properties

        public BitfinexSocketApi()
        {
            this.socket = new WebSocket("wss://api.bitfinex.com/ws/1", sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls);
            this.socket.Opened += this.Socket_Opened;
            this.socket.MessageReceived += this.Socket_MessageReceived;
            this.socket.Error += this.Socket_Error;
            this.socket.Closed += this.Socket_Closed;

            this.channelsCache = new Dictionary<string, Dictionary<BitfinexChannelType, int>>();
            this.channelTypeCache = new Dictionary<int, BitfinexChannelType>();
            this.symbolByChannelCache = new Dictionary<int, string>();
        }

        public void Connect()
        {
            Core.Instance.Loggers.Log($"----------> Bitfinex. Start connecting", LoggingLevel.Verbose);

            this.ConnectionState = BitfinexConnectionState.Connecting;
            this.socket.Open();

            while (this.ConnectionState == BitfinexConnectionState.Connecting)
                System.Threading.Thread.Sleep(100);
        }

        public void Disconnect() => this.socket.Close();

        #region Subscriptions
        public void SubscribeTicker(string pair)
        {
            var request = new BitfinexSocketRequestResponse
            {
                Event = BitfinexEvent.Subscribe,
                Channel = BitfinexChannelType.Ticker,
                Pair = pair
            };

            SendRequest(request);
        }

        public void UnsubscribeTicker(string pair)
        {
            if (!TryGetChannelId(BitfinexChannelType.Ticker, pair, out int channelId))
                return;

            var request = new BitfinexSocketRequestResponse
            {
                Event = BitfinexEvent.Unsubscribe,
                ChannelId = channelId
            };

            SendRequest(request);
        }

        public void SubscribeBook(string pair)
        {
            var request = new BitfinexSocketRequestResponse
            {
                Event = BitfinexEvent.Subscribe,
                Channel = BitfinexChannelType.Book,
                Pair = pair,
                BookLength = 100
            };

            SendRequest(request);
        }

        public void UnsubscribeBook(string pair)
        {
            if (!TryGetChannelId(BitfinexChannelType.Book, pair, out int channelId))
                return;

            var request = new BitfinexSocketRequestResponse
            {
                Event = BitfinexEvent.Unsubscribe,
                ChannelId = channelId
            };

            SendRequest(request);
        }

        public void SubscribeTrades(string pair)
        {
            var request = new BitfinexSocketRequestResponse
            {
                Event = BitfinexEvent.Subscribe,
                Channel = BitfinexChannelType.Trades,
                Pair = pair
            };

            SendRequest(request);
        }

        public void UnsubscribeTrades(string pair)
        {
            if (!TryGetChannelId(BitfinexChannelType.Trades, pair, out int channelId))
                return;

            var request = new BitfinexSocketRequestResponse
            {
                Event = BitfinexEvent.Unsubscribe,
                ChannelId = channelId
            };

            SendRequest(request);
        }
        #endregion Subscriptions

        #region Misc
        private void SendRequest(BitfinexSocketRequestResponse request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                this.socket.Send(json);
            }
            catch(Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }
            finally
            {

            }
        }

        private bool TryGetChannelId(BitfinexChannelType channelType, string pair, out int channelId)
        {
            channelId = -1;

            return this.channelsCache.TryGetValue(pair, out var cache) && cache.TryGetValue(channelType, out channelId);
        }

        private void Socket_Opened(object sender, EventArgs e)
        {
            Core.Instance.Loggers.Log($"----------> Bitfinex. Socket opened", LoggingLevel.Verbose);

            this.ConnectionState = BitfinexConnectionState.Connected;
        }

        private void Socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Core.Instance.Loggers.Log($"----------> Bitfinex. Message received: {e.Message}", LoggingLevel.Verbose);

            try
            {
                JToken jToken = JToken.Parse(e.Message);

                if (jToken is JObject jObject)
                {
                    var @event = jObject["event"]?.ToObject<BitfinexEvent>();

                    if (@event != null)
                    {
                        switch (@event)
                        {
                            case BitfinexEvent.Subscribed:
                            case BitfinexEvent.Unsubscribed:
                                var response = jObject.ToObject<BitfinexSocketRequestResponse>();
                                var key = new BitfinexRequestKey
                                {
                                    Channel = response.Channel,
                                    Pair = response.Pair
                                };

                                if (response.Event == BitfinexEvent.Subscribed)
                                {
                                    if (!this.channelsCache.TryGetValue(response.Pair, out Dictionary<BitfinexChannelType, int> cache))
                                        this.channelsCache.Add(response.Pair, cache = new Dictionary<BitfinexChannelType, int>());

                                    cache[response.Channel] = response.ChannelId;

                                    this.channelTypeCache[response.ChannelId] = response.Channel;
                                    this.symbolByChannelCache[response.ChannelId] = response.Pair;
                                }
                                else if (response.Event == BitfinexEvent.Unsubscribed)
                                {
                                    string symbol = this.symbolByChannelCache[response.ChannelId];

                                    this.channelsCache[symbol].Remove(response.Channel);
                                    this.channelTypeCache.Remove(response.ChannelId);
                                    this.symbolByChannelCache.Remove(response.ChannelId);
                                }
                                break;
                        }
                    }
                }
                else if (jToken is JArray jArray)
                {
                    if (jArray[1].ToString() == "hb")
                        return;

                    int channelId = jArray[0].Value<int>();

                    if (!this.channelTypeCache.TryGetValue(channelId, out var channelType) || !this.symbolByChannelCache.TryGetValue(channelId, out var symbol))
                        return;

                    switch(channelType)
                    {
                        case BitfinexChannelType.Ticker:
                            var ticker = new BitfinexTicker
                            {
                                Pair = symbol,
                                Bid = jArray[1].Value<decimal>(),
                                BidSize = jArray[2].Value<decimal>(),
                                Ask = jArray[3].Value<decimal>(),
                                AskSize = jArray[4].Value<decimal>(),
                                DailyChange = jArray[5].Value<decimal>(),
                                DailyChangePercent = jArray[6].Value<decimal>(),
                                LastPrice = jArray[7].Value<decimal>(),
                                Volume = jArray[8].Value<decimal>(),
                                High = jArray[9].Value<decimal>(),
                                Low = jArray[10].Value<decimal>()
                            };

                            this.NewData?.Invoke(this, new BitfinexEventArgs { Ticker = ticker });
                            break;
                        case BitfinexChannelType.Book:
                            if (jArray.Count == 2)
                            {
                                List<BitfinexBookItem> book = new List<BitfinexBookItem>();

                                foreach(var item in jArray[1])
                                {
                                    book.Add(new BitfinexBookItem
                                    {
                                        Pair = symbol,
                                        Price = item[0].Value<decimal>(),
                                        Count = item[1].Value<int>(),
                                        Amount = item[2].Value<decimal>()
                                    });
                                }

                                this.NewData?.Invoke(this, new BitfinexEventArgs { Book = book });
                            }
                            else
                            {
                                var bookItem = new BitfinexBookItem
                                {
                                    Pair = symbol,
                                    Price = jArray[1].Value<decimal>(),
                                    Count = jArray[2].Value<int>(),
                                    Amount = jArray[3].Value<decimal>()
                                };

                                this.NewData?.Invoke(this, new BitfinexEventArgs { BookItem = bookItem });
                            }
                            break;
                        case BitfinexChannelType.Trades:
                            if (jArray.Count == 2)
                            {
                                var lastTrade = jArray[1][0];

                                var trade = new BitfinexTrade
                                {
                                    Pair = symbol,
                                    Timestamp = lastTrade[1].Value<long>(),
                                    Price = lastTrade[2].Value<decimal>(),
                                    Amount = lastTrade[3].Value<decimal>()
                                };
                                this.NewData?.Invoke(this, new BitfinexEventArgs { Trade = trade, IsSnapshotData = true });
                            }
                            else
                            {
                                if (jArray[1].Value<string>() == "tu")
                                    return;

                                var trade = new BitfinexTrade
                                {
                                    Pair = symbol,
                                    Timestamp = jArray[3].Value<long>(),
                                    Price = jArray[4].Value<decimal>(),
                                    Amount = jArray[5].Value<decimal>()
                                };

                                this.NewData?.Invoke(this, new BitfinexEventArgs { Trade = trade });
                            }
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }
        }

        private void Socket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Core.Instance.Loggers.Log($"----------> Bitfinex. Socket error: {e.Exception.InnerException?.Message ?? e.Exception.Message}", LoggingLevel.Verbose);
        }

        private void Socket_Closed(object sender, EventArgs e)
        {
            Core.Instance.Loggers.Log($"----------> Bitfinex. Socket closed", LoggingLevel.Verbose);

            this.ConnectionState = BitfinexConnectionState.Disconnected;
        }
        #endregion Misc
    }
}