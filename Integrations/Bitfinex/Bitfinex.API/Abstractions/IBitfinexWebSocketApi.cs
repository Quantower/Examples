// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Threading;
using Bitfinex.API.Models;

namespace Bitfinex.API.Abstractions;

public interface IBitfinexWebSocketApi
{
    bool IsOpened { get; }

    event EventHandler<BitfinexEventArgs> NewData;
    event EventHandler<BitfinexErrorEventArgs> Error;

    void Connect(CancellationToken cancellation);

    void Disconnect();

    void Ping(CancellationToken cancellation);
}