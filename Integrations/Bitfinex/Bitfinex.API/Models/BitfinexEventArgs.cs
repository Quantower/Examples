// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;
using System.Collections.Generic;

namespace Bitfinex.API.Models;

public class BitfinexEventArgs : EventArgs
{
    public string Event { get; internal set; }

    public BitfinexTicker Ticker { get; internal set; }

    public BitfinexBookItem BookItem { get; internal set; }

    public List<BitfinexBookItem> Book { get; internal set; }

    public BitfinexTrade Trade { get; internal set; }

    public bool IsSnapshotData { get; internal set; }

    public BitfinexWallet WalletUpdate { get; internal set; }

    public BitfinexOrder OrderUpdate { get; internal set; }

    public BitfinexPosition PositionUpdate { get; internal set; }

    public BitfinexUserTrade UserTrade { get; internal set; }

    public BitfinexNotification Notification { get; internal set; }
}