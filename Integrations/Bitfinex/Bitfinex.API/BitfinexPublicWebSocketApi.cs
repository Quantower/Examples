// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Bitfinex.API.Abstractions;
using Bitfinex.API.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bitfinex.API;

internal class BitfinexPublicWebSocketApi : BitfinexWebSocketApi, IBitfinexPublicWebSocketApi
{
    private readonly IDictionary<BitfinexSubscriptionKey, string> channelIdCache;
    private readonly IDictionary<string, BitfinexSubscriptionKey> subscriptionKeyCache;

    public BitfinexPublicWebSocketApi(string endpoint)
        : base(endpoint)
    {
        this.channelIdCache = new Dictionary<BitfinexSubscriptionKey, string>();
        this.subscriptionKeyCache = new Dictionary<string, BitfinexSubscriptionKey>();
    }

    public void Subscribe(string channel, string pair, CancellationToken cancellation, out string error)
    {
        error = default;

        try
        {
            var message = new BitfinexMessage
            {
                Event = BitfinexEvent.SUBSCRIBE,
                ChannelName = channel,
                Pair = pair
            };

            if (channel == BitfinexChannel.BOOK)
                message.Precision = "P0";

            var response = this.SendMessage(message, cancellation);

            if (response == null)
            {
                error = "Response is null";
                return;
            }

            if (response.Event == BitfinexEvent.ERROR)
            {
                error = response.FormatError();
                return;
            }

            if (string.IsNullOrEmpty(response.ChannelId))
            {
                error = "Channel id is empty";
                return;
            }

            var key = new BitfinexSubscriptionKey(channel, pair);

            this.channelIdCache[key] = response.ChannelId;
            this.subscriptionKeyCache[response.ChannelId] = key;
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

    public void Unsubscribe(string channel, string pair, CancellationToken cancellation, out string error)
    {
        error = default;

        try
        {
            var key = new BitfinexSubscriptionKey(channel, pair);

            if (!this.channelIdCache.TryGetValue(key, out string channelId))
            {
                error = $"Can't find channel id by key: {key}";
                return;
            }

            var message = new BitfinexMessage
            {
                Event = BitfinexEvent.UNSUBSCRIBE,
                ChannelId = channelId
            };

            var response = this.SendMessage(message, cancellation);

            if (response == null)
            {
                error = "Response is null";
                return;
            }

            if (response.Event == BitfinexEvent.ERROR)
            {
                error = response.FormatError();
                return;
            }

            this.channelIdCache.Remove(key);
            this.subscriptionKeyCache.Remove(channelId);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

    private protected override void DeserializeData(JArray message)
    {
        if (message[1].ToString() == "hb")
            return;

        string channelId = message[0].Value<string>();

        if (string.IsNullOrEmpty(channelId))
            return;

        if (!this.subscriptionKeyCache.TryGetValue(channelId, out var key))
            return;

        switch (key.Channel)
        {
            case BitfinexChannel.TICKER:
                var ticker = new BitfinexTicker
                {
                    Pair = key.Pair,
                    Bid = message[1].Value<decimal>(),
                    BidSize = message[2].Value<decimal>(),
                    Ask = message[3].Value<decimal>(),
                    AskSize = message[4].Value<decimal>(),
                    DailyChange = message[5].Value<decimal>(),
                    DailyChangePercent = message[6].Value<decimal>(),
                    LastPrice = message[7].Value<decimal>(),
                    Volume = message[8].Value<decimal>(),
                    High = message[9].Value<decimal>(),
                    Low = message[10].Value<decimal>()
                };

                this.OnNewData(new BitfinexEventArgs { Ticker = ticker });
                break;
            case BitfinexChannel.BOOK:
                if (message.Count == 2)
                {
                    var book = new List<BitfinexBookItem>();

                    foreach (var item in message[1])
                    {
                        book.Add(new BitfinexBookItem
                        {
                            Pair = key.Pair,
                            Price = item[0].Value<decimal>(),
                            Count = item[1].Value<int>(),
                            Amount = item[2].Value<decimal>()
                        });
                    }

                    this.OnNewData(new BitfinexEventArgs { Book = book });
                }
                else
                {
                    var bookItem = new BitfinexBookItem
                    {
                        Pair = key.Pair,
                        Price = message[1].Value<decimal>(),
                        Count = message[2].Value<int>(),
                        Amount = message[3].Value<decimal>()
                    };

                    this.OnNewData(new BitfinexEventArgs { BookItem = bookItem });
                }
                break;
            case BitfinexChannel.TRADES:
                if (message.Count == 2)
                {
                    var data = message[1].Value<JArray>();
                    if (data == null || !data.Any())
                        return;

                    var lastTrade = data[0];

                    var trade = new BitfinexTrade
                    {
                        Pair = key.Pair,
                        Timestamp = lastTrade[1].Value<long>(),
                        Price = lastTrade[2].Value<decimal>(),
                        Amount = lastTrade[3].Value<decimal>()
                    };
                    this.OnNewData(new BitfinexEventArgs { Trade = trade, IsSnapshotData = true });
                }
                else
                {
                    if (message[1].Value<string>() == "te")
                        return;

                    var trade = new BitfinexTrade
                    {
                        Pair = key.Pair,
                        Id = message[3].Value<long>(),
                        Timestamp = message[4].Value<long>(),
                        Price = message[5].Value<decimal>(),
                        Amount = message[6].Value<decimal>()
                    };

                    this.OnNewData(new BitfinexEventArgs { Trade = trade });
                }
                break;
        }
    }

    #region Utilities

    private readonly struct BitfinexSubscriptionKey : IEquatable<BitfinexSubscriptionKey>
    {
        public string Channel { get; }
        public string Pair { get; }

        public BitfinexSubscriptionKey(string channel, string pair)
        {
            this.Channel = channel;
            this.Pair = pair;
        }

        public bool Equals(BitfinexSubscriptionKey other) =>
            this.Channel == other.Channel && this.Pair == other.Pair;

        public override bool Equals(object obj) =>
            obj is BitfinexSubscriptionKey other && this.Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Channel != null ? this.Channel.GetHashCode() : 0) * 397) ^ (this.Pair != null ? this.Pair.GetHashCode() : 0);
            }
        }

        public override string ToString() => $"{this.Channel} - {this.Pair}";
    }

    #endregion Utilities
}