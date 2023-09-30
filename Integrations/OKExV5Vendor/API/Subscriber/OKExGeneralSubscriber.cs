// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket.Models;
using System;

namespace OKExV5Vendor.API.Subscriber;

class OKExGeneralSubscriber : OKExSubscriberBase<OKExTicker>
{
    public OKExOpenInterest OpenInterest { get; internal set; }
    public OKExTradeItem LastTrade { get; internal set; }
    public OKExQuote LastQuote { get; private set; }

    public OKExGeneralSubscriber(OKExSymbol symbol)
        : base(symbol) { }

    internal bool TryUpdateTicker(OKExTicker ticker)
    {
        bool needUpdateDaybar;

        if (this.LastTicker == null)
        {
            needUpdateDaybar = true;
        }
        else
        {
            needUpdateDaybar = ticker.LastPrice != this.LastTicker.LastPrice ||
                               ticker.OpenPrice24h != this.LastTicker.OpenPrice24h ||
                               ticker.HighPrice24h != this.LastTicker.HighPrice24h ||
                               ticker.LowPrice24h != this.LastTicker.LowPrice24h ||
                               ticker.Volume24h != this.LastTicker.Volume24h ||
                               ticker.VolumeCurrency24h != this.LastTicker.VolumeCurrency24h;
        }

        this.LastTicker = ticker;
        return needUpdateDaybar;
    }
    internal bool TryUpdateQuote(ref OKExOrderBookItem bid, ref OKExOrderBookItem ask, DateTime time, out OKExQuote quote)
    {
        quote = null;

        if (this.LastQuote == null ||
            this.LastQuote.AskPrice != ask.Price ||
            this.LastQuote.AskSize != ask.Size ||
            this.LastQuote.BidPrice != bid.Price ||
            this.LastQuote.BidSize != bid.Size)
        {
            this.LastQuote = quote = new OKExQuote(bid, ask, time);
        }

        return quote != null;
    }

    internal bool ContainsAnyMainSubscription()
    {
        if (this.Symbol.InstrumentType == OKExInstrumentType.Spot)
            return !(this.ContainsSubscription(OKExSubscriptionType.Ticker) && this.SubscriptionCount == 1);
        else
            return !(this.ContainsSubscription(OKExSubscriptionType.Ticker) && this.ContainsSubscription(OKExSubscriptionType.OpenInterest) && this.SubscriptionCount == 2);
    }
}