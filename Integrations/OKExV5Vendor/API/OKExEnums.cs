using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace OKExV5Vendor.API
{
    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExInstrumentType
    {
        [EnumMember(Value = "ANY")]
        Any,
        [EnumMember(Value = "SPOT")]
        Spot,
        [EnumMember(Value = "SWAP")]
        Swap,
        [EnumMember(Value = "FUTURES")]
        Futures,
        [EnumMember(Value = "OPTION")]
        Option,
        [EnumMember(Value = "MARGIN")]
        Margin,
        Index
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExOptionType
    {
        Undefined = 0,
        [EnumMember(Value = "C")]
        Call,
        [EnumMember(Value = "P")]
        Put,
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExContractType
    {
        Undefined = 0,
        [Description("Linear")]
        [EnumMember(Value = "linear")]
        Linear,
        [Description("Inverse")]
        [EnumMember(Value = "inverse")]
        Inverse,
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExFutureAliasType
    {
        Undefined = 0,
        [Description("This week")]
        [EnumMember(Value = "this_week")]
        ThisWeek,
        [Description("Next week")]
        [EnumMember(Value = "next_week")]
        NextWeek,
        [Description("Quarter")]
        [EnumMember(Value = "quarter")]
        Quarter,
        [Description("Next quarter")]
        [EnumMember(Value = "next_quarter")]
        NextQuarter
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExInstrumentStatus
    {
        Undefined = 0,
        [EnumMember(Value = "live")]
        Live,
        [EnumMember(Value = "suspend")]
        Suspend,
        [EnumMember(Value = "expired")]
        Expired,
        [EnumMember(Value = "preopen")]
        PreOpen,
    }

    [Obfuscation(Exclude = true)]
    enum OKExCandlePeriod
    {
        Tick1,
        [EnumMember(Value = "1m")]
        Min1,
        [EnumMember(Value = "3m")]
        Min3,
        [EnumMember(Value = "5m")]
        Min5,
        [EnumMember(Value = "15m")]
        Min15,
        [EnumMember(Value = "30m")]
        Min30,
        [EnumMember(Value = "1H")]
        Hour1,
        [EnumMember(Value = "2H")]
        Hour2,
        [EnumMember(Value = "4H")]
        Hour4,
        [EnumMember(Value = "6H")]
        Hour6,
        [EnumMember(Value = "12H")]
        Hour12,
        [EnumMember(Value = "1D")]
        Day1,
        [EnumMember(Value = "1W")]
        Week1,
        [EnumMember(Value = "1M")]
        Month1,
        [EnumMember(Value = "3M")]
        Month3,
        [EnumMember(Value = "6M")]
        Month6,
        [EnumMember(Value = "1Y")]
        Year1,
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExHistoryType
    {
        Last,
        Mark
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExSide
    {
        [EnumMember(Value = "buy")]
        Buy,
        [EnumMember(Value = "sell")]
        Sell
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExPositionSide
    {
        [EnumMember(Value = "long")]
        Long,
        [EnumMember(Value = "short")]
        Short,
        [EnumMember(Value = "net")]
        Net
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExTradeMode
    {
        [EnumMember(Value = "cash")]
        Cash,
        [EnumMember(Value = "cross")]
        Cross,
        [EnumMember(Value = "isolated")]
        Isolated
    }

    [Obfuscation(Exclude = true)]
    enum OKExSubscriptionType
    {
        Last,
        Mark,
        Quote,
        Level2,
        Ticker,
        OpenInterest
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExOrderType
    {
        [EnumMember(Value = "market")]
        Market,
        [EnumMember(Value = "limit")]
        Limit,
        [EnumMember(Value = "post_only")]
        PostOnly,
        [EnumMember(Value = "fok")]
        FOK,
        [EnumMember(Value = "ioc")]
        IOC,
        [EnumMember(Value = "optimal_limit_ioc")]
        OptLimitIOC,
    }

    [Obfuscation(Exclude = true)]
    enum OKExAccountLevel
    {
        [Description("Undefined")]
        Undefined,
        [Description("Simple")]
        Simple,
        [Description("Single currency margin")]
        SingleCurrencyMargin,
        [Description("Multi currency margin")]
        MultiCurrencyMargin
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExPositionMode
    {
        [Description("Long/Short")]
        [EnumMember(Value = "long_short_mode")]
        LongShort,
        [Description("Net")]
        [EnumMember(Value = "net_mode")]
        Net,
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExGreeksType
    {
        [EnumMember(Value = "PA")]
        PA,
        [EnumMember(Value = "BS")]
        BS
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExAlgoOrderType
    {
        [EnumMember(Value = "conditional")]
        Conditional,
        [EnumMember(Value = "oco")]
        OCO,
        [EnumMember(Value = "trigger")]
        Trigger
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExOrderState
    {
        Undefined,
        [EnumMember(Value = "live")]
        Live,
        [EnumMember(Value = "canceled")]
        Canceled,
        [EnumMember(Value = "partially_filled")]
        PartiallyFilled,
        [EnumMember(Value = "filled")]
        Filled
    }

    [Obfuscation(Exclude = true)]
    [JsonConverter(typeof(StringEnumConverter))]
    enum OKExAlgoOrderState
    {
        [EnumMember(Value = "live")]
        Live,
        [EnumMember(Value = "canceled")]
        Canceled,
        [EnumMember(Value = "effective")]
        Effective,
        [EnumMember(Value = "order_failed")]
        OrderFailed
    }

    [Obfuscation(Exclude = true)]
    enum OKExSLTPType
    {
        Market,
        Limit
    }

    [Obfuscation(Exclude = true)]
    enum OKExDepositState
    {
        [Description("Undefined")]
        Undefined = 0,
        [Description("Waiting for confirmation")]
        WaitingForConfirm,
        [Description("Created")]
        Created,
        [Description("Successful")]
        Successful
    }

    [Obfuscation(Exclude = true)]
    enum OKExWithdrawalState
    {
        [Description("Undefined")]
        Undefined = -999,
        [Description("Pending cancel")]
        PendingCancel = -3,
        [Description("Canceled")]
        Canceled = -2,
        [Description("Failed")]
        Failed = -1,
        [Description("Pending")]
        Pending = 0,
        [Description("Sending")]
        Sending = 1,
        [Description("Sent")]
        Sent = 2,
        [Description("Awaiting email verification")]
        AwaitingEmailVerification = 3,
        [Description("Awaiting manual verification")]
        AwaitingManualVerification = 4,
        [Description("Awaiting identity verification")]
        AwaitingIdentityVerification = 5,
    }

    [Obfuscation(Exclude = true)]
    enum OKExOrderBehaviourType
    {
        Open,
        Close
    }
}
