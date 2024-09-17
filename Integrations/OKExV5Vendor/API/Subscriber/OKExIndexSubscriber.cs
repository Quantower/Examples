// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using OKExV5Vendor.API.REST.Models;

namespace OKExV5Vendor.API.Subscriber;

class OKExIndexSubscriber : OKExSubscriberBase<OKExIndexTicker>
{
    public OKExIndexSubscriber(OKExSymbol symbol)
        : base(symbol) { }

    internal bool TryUpdateTicker(OKExIndexTicker ticker, out bool isIndexPriceChanged)
    {
        bool needUpdateDaybar;

        if (this.LastTicker == null)
        {
            needUpdateDaybar = true;
            isIndexPriceChanged = false;
        }
        else
        {
            isIndexPriceChanged = ticker.IndexPrice != this.LastTicker.IndexPrice;

            needUpdateDaybar = ticker.OpenPrice24h != this.LastTicker.OpenPrice24h ||
                               ticker.HighPrice24h != this.LastTicker.HighPrice24h ||
                               ticker.LowPrice24h != this.LastTicker.LowPrice24h;
        }

        this.LastTicker = ticker;

        return needUpdateDaybar;
    }
}