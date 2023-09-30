// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Threading;

namespace Bitfinex.API.Abstractions;

public interface IBitfinexPrivateWebSocketApi : IBitfinexWebSocketApi
{
    int? Authenticate(string apiKey, string apiSecret, out string error, CancellationToken cancellation);
}