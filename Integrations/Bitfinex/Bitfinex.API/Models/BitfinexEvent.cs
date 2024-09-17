// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

namespace Bitfinex.API.Models;

public static class BitfinexEvent
{
    public const string INFO = "info";
    public const string ERROR = "error";
    public const string SUBSCRIBE = "subscribe";
    public const string SUBSCRIBED = "subscribed";
    public const string UNSUBSCRIBE = "unsubscribe";
    public const string UNSUBSCRIBED = "unsubscribed";
    public const string PING = "ping";
    public const string PONG = "pong";
    public const string AUTH = "auth";
    public const string HEARTBEAT = "hb";
    public const string WALLET_UPDATE = "wu";
    public const string ORDER_NEW = "on";
    public const string ORDER_UPDATE = "ou";
    public const string ORDER_CANCEL = "oc";
    public const string POSITION_NEW = "pn";
    public const string POSITION_UPDATE = "pu";
    public const string POSITION_CLOSE = "pc";
    public const string TRADE_EXECUTED = "te";
    public const string TRADE_EXECUTION_UPDATED = "tu";
    public const string NOTIFICATION = "n";
}