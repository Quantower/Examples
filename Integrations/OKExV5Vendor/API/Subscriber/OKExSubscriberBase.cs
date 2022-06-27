// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using OKExV5Vendor.API.REST.Models;
using System.Collections.Generic;

namespace OKExV5Vendor.API.Subscriber
{
    abstract class OKExSubscriberBase<T>
    {
        public OKExSymbol Symbol { get; private set; }

        private readonly HashSet<OKExSubscriptionType> channelsCache;
        public int SubscriptionCount => this.channelsCache.Count;

        public T LastTicker { get; protected set; }

        public OKExSubscriberBase(OKExSymbol symbol)
        {
            this.Symbol = symbol;
            this.channelsCache = new HashSet<OKExSubscriptionType>();
        }

        internal void AddSubscription(OKExSubscriptionType subscriptionType)
        {
            this.channelsCache.Add(subscriptionType);
        }
        internal void RemoveChannel(OKExSubscriptionType subscriptionType)
        {
            this.channelsCache.Remove(subscriptionType);
        }
        internal bool ContainsSubscription(OKExSubscriptionType subscriptionType)
        {
            return this.channelsCache.Contains(subscriptionType);
        }
    }
}
