// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System.Threading;

namespace Bitfinex.API.Abstractions
{
    public interface IBitfinexPublicWebSocketApi : IBitfinexWebSocketApi
    {
        void Subscribe(string channel, string pair, CancellationToken cancellation, out string error);

        void Unsubscribe(string channel, string pair, CancellationToken cancellation, out string error);
    }
}