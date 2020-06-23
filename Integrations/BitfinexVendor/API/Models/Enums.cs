// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace BitfinexVendor.API
{
    enum BitfinexConnectionState
    {
        Idle,
        Connecting,
        Connected,
        Disconnected
    }

    [JsonConverter(typeof(StringEnumConverter))]
    enum BitfinexEvent
    {
        [EnumMember(Value = "info")]
        Info,
        [EnumMember(Value = "error")]
        Error,
        [EnumMember(Value = "subscribe")]
        Subscribe,
        [EnumMember(Value = "subscribed")]
        Subscribed,
        [EnumMember(Value = "unsubscribe")]
        Unsubscribe,
        [EnumMember(Value = "unsubscribed")]
        Unsubscribed
    }

    [JsonConverter(typeof(StringEnumConverter))]
    enum BitfinexChannelType
    {
        Unknown,
        [EnumMember(Value = "ticker")]
        Ticker,
        [EnumMember(Value = "trades")]
        Trades,
        [EnumMember(Value = "book")]
        Book
    }
}
