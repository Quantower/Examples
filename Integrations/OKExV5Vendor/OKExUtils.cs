using OKExV5Vendor.API;
using OKExV5Vendor.API.OrderType;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket;
using OKExV5Vendor.API.Websocket.Models;
using System;
using System.Globalization;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace OKExV5Vendor
{
    class OKExConsts
    {
        public const string VENDOR_NAME = "OKEx";
        public const string DEFAULT_EXCHANGE_ID = "1";

        public const int MAX_CLIENT_ORDER_ID_LENGTH = 32;
        public const int MAX_COMMENT_LENGHT = 8;
        public const string BROKER_ID = "8813794bd2ee4eBC";

        public const string ORDER_BOOK_SNAPSHOT = "snapshot";
        public const string ORDER_BOOK_UPDATE = "update";

        public const string CONNECTION_INFO = "Info";
        public const string CONNECTION_TRADING = "Trading";
        public const string PARAMETER_API_KEY = "ApyKey";
        public const string PARAMETER_SECRET_ID = "Secret";
        public const string PARAMETER_PASSPHRASE_ID = "Passphrase";

        public const string GENERAL_GROUP = "#10.General";
        public const string TRADING_INFO_GROUP = "#20.Trading info";
        public const string ACCOUNT_ACTIVITY_GROUP = "#40.Account activity";

        public const string REPORT_TYPE_PARAMETER_ALGO_ORDER_TYPE = "Algo order type";
        public const string REPORT_TYPE_PARAMETER_ALGO_ORDER_STATE = "Algo order state";

        public const int GET_ORDERS_REPORTS_ID = 0;
        public const int GET_ALGO_ORDERS_REPORTS_ID = 1;
        public const int GET_DEPOSIT_REPORTS_ID = 2;
        public const int GET_WITHDRAWAL_REPORTS_ID = 3;

        public const string LOGIN = "login";
        public const string SUBSCRIBE = "subscribe";
        public const string UNSUBSCRIBE = "unsubscribe";
        public const string ERROR = "error";

        public static OKExClientSettings RealServer => new OKExClientSettings("https://www.okex.com", "wss://ws.okex.com:8443/ws/v5/public", "wss://ws.okex.com:8443/ws/v5/private", false);
        public static OKExClientSettings DemoServer => new OKExClientSettings("https://www.okex.com", "wss://wspap.okex.com:8443/ws/v5/public?brokerId=9999", "wss://wspap.okex.com:8443/ws/v5/private?brokerId=9999", true);
        public static Period[] AllowedPeriods => OKExExtension.GetAvailablePeriods();
    }

    static class OKExExtension
    {
        private static readonly Map<Period, OKExCandlePeriod> periodMapper = new Map<Period, OKExCandlePeriod>
        {
            { Period.TICK1, OKExCandlePeriod.Tick1 },
            { Period.MIN1, OKExCandlePeriod.Min1 },
            { Period.MIN3, OKExCandlePeriod.Min3 },
            { Period.MIN5, OKExCandlePeriod.Min5 },
            { Period.MIN15, OKExCandlePeriod.Min15 },
            { Period.MIN30, OKExCandlePeriod.Min30 },
            { Period.HOUR1, OKExCandlePeriod.Hour1 },
            { Period.HOUR2, OKExCandlePeriod.Hour2 },
            { Period.HOUR4, OKExCandlePeriod.Hour4 },
            { Period.HOUR6, OKExCandlePeriod.Hour6 },
            { Period.HOUR12, OKExCandlePeriod.Hour12 },
            { Period.DAY1, OKExCandlePeriod.Day1 },
            //{ Period.WEEK1, OKExCandlePeriod.Week1 },
            { Period.MONTH1, OKExCandlePeriod.Month1 },
            { new Period(BasePeriod.Month, 3), OKExCandlePeriod.Month3 },
            { new Period(BasePeriod.Month, 6), OKExCandlePeriod.Month6 },
            { Period.YEAR1, OKExCandlePeriod.Year1 }
        };

        public static SymbolType ToTerminal(this OKExInstrumentType type)
        {
            return type switch
            {
                OKExInstrumentType.Spot => SymbolType.Crypto,
                OKExInstrumentType.Swap => SymbolType.Swap,
                OKExInstrumentType.Futures => SymbolType.Futures,
                OKExInstrumentType.Option => SymbolType.Options,
                OKExInstrumentType.Index => SymbolType.Indexes,

                _ => throw new ArgumentException($"Unsupported symbol type - {type}"),
            };
        }
        public static OKExInstrumentType ToOKEx(this SymbolType type)
        {
            return type switch
            {
                SymbolType.Crypto => OKExInstrumentType.Spot,
                SymbolType.Swap => OKExInstrumentType.Swap,
                SymbolType.Futures => OKExInstrumentType.Futures,
                SymbolType.Options => OKExInstrumentType.Option,
                SymbolType.Indexes => OKExInstrumentType.Index,

                _ => throw new ArgumentException($"Unsupported symbol type - {type}"),
            };
        }

        public static OptionType ToTerminal(this OKExOptionType type)
        {
            return type switch
            {
                OKExOptionType.Call => OptionType.Call,
                OKExOptionType.Put => OptionType.Put,

                _ => throw new ArgumentException($"Unsupported option type - {type}"),
            };
        }
        public static Side ToTerminal(this OKExSide side)
        {
            return side switch
            {
                OKExSide.Buy => Side.Buy,
                OKExSide.Sell => Side.Sell,

                _ => throw new ArgumentException($"Unsupported side - {side}"),
            };
        }
        public static OrderStatus ToTerminal(this OKExOrderState state)
        {
            return state switch
            {
                OKExOrderState.Live => OrderStatus.Opened,
                OKExOrderState.Canceled => OrderStatus.Cancelled,
                OKExOrderState.PartiallyFilled => OrderStatus.PartiallyFilled,
                OKExOrderState.Filled => OrderStatus.Filled,

                _ => OrderStatus.Unspecified,
            };
        }
        public static OrderStatus ToTerminal(this OKExAlgoOrderState state)
        {
            return state switch
            {
                OKExAlgoOrderState.Live => OrderStatus.Opened,
                OKExAlgoOrderState.Canceled => OrderStatus.Cancelled,
                OKExAlgoOrderState.Effective => OrderStatus.Opened,
                OKExAlgoOrderState.OrderFailed => OrderStatus.Refused,

                _ => OrderStatus.Unspecified,
            };
        }

        public static string ToTerminalOrderType(this OKExOrder order)
        {
            switch (order.OrderType)
            {
                case OKExOrderType.Market:
                case OKExOrderType.OptLimitIOC:
                    return OrderType.Market;
                case OKExOrderType.Limit:
                case OKExOrderType.PostOnly:
                case OKExOrderType.FOK:
                case OKExOrderType.IOC:
                    return OrderType.Limit;

                default:
                    throw new ArgumentException($"Unsupported order type - {order.OrderType}");
            }
        }
        public static TimeInForce ToTerminalTIF(this OKExOrder order)
        {
            return order.OrderType switch
            {
                OKExOrderType.FOK => TimeInForce.FOK,
                OKExOrderType.IOC or OKExOrderType.OptLimitIOC => TimeInForce.IOC,

                _ => TimeInForce.Default,
            };
        }
        public static Side ToTerminalSide(this OKExPosition position, OKExSymbol symbol)
        {
            switch (position.PositionSide)
            {
                case OKExPositionSide.Long:
                    return Side.Buy;
                case OKExPositionSide.Short:
                    return Side.Sell;
                case OKExPositionSide.Net:
                    {
                        if (position.InstrumentType == OKExInstrumentType.Margin)
                        {
                            if (position.PositionCurrency == symbol.BaseCurrency)
                                return Side.Buy;
                            else if (position.PositionCurrency == symbol.QuoteCurrency)
                                return Side.Sell;
                        }
                        else
                        {
                            if (position.Quantity > 0)
                                return Side.Buy;
                            else
                                return Side.Sell;
                        }

                        break;
                    }
            }

            throw new ArgumentException($"Unsupported position side - {position.PositionSide}");
        }
        public static OKExPositionSide ToPositionSide(this OKExSide oKExSide)
        {
            return oKExSide switch
            {
                OKExSide.Buy => OKExPositionSide.Long,
                OKExSide.Sell => OKExPositionSide.Short,

                _ => throw new ArgumentException($"Unsupported okex side - {oKExSide}"),
            };
        }
        public static OKExPositionSide Revers(this OKExPositionSide oKExPositionSide)
        {
            return oKExPositionSide switch
            {
                OKExPositionSide.Long => OKExPositionSide.Short,
                OKExPositionSide.Short => OKExPositionSide.Long,

                _ => OKExPositionSide.Net,
            };
        }
        public static OKExCandlePeriod ToOKEx(this Period period)
        {
            if (periodMapper.TryGetDirect(period, out var okexPeriod))
                return okexPeriod;
            else
                throw new ArgumentException($"Unsupported candle period - {period}");
        }
        public static OKExHistoryType ToOKEx(this HistoryType type)
        {
            switch (type)
            {
                case HistoryType.Last:
                    return OKExHistoryType.Last;
                case HistoryType.Mark:
                    return OKExHistoryType.Mark;

                default:
                    throw new ArgumentException($"Unsupported history type - {type}");
            }
        }
        public static OKExSide ToOKEx(this Side side)
        {
            return side switch
            {
                Side.Buy => OKExSide.Buy,
                Side.Sell => OKExSide.Sell,

                _ => throw new ArgumentException($"Unsupported position side - {side}"),
            };
        }
        public static OKExOrderType ToOKExOrderType(this PlaceOrderRequestParameters parameters)
        {
            if (parameters.OrderTypeId == OrderType.Limit)
            {
                bool postOnly = parameters.AdditionalParameters.GetVisibleValue<bool>(OKExOrderTypeHelper.POST_ONLY);

                if (postOnly)
                    return OKExOrderType.PostOnly;

                if (parameters.TimeInForce == TimeInForce.FOK)
                    return OKExOrderType.FOK;
                else if (parameters.TimeInForce == TimeInForce.IOC)
                    return OKExOrderType.IOC;

                return OKExOrderType.Limit;
            }
            else if (parameters.OrderTypeId == OrderType.Market)
            {
                if (parameters.TimeInForce == TimeInForce.IOC)
                    return OKExOrderType.OptLimitIOC;
                else
                    return OKExOrderType.Market;
            }

            throw new ArgumentException($"Unsupported order type - {parameters.OrderTypeId}");
        }
        public static OKExAlgoOrderType ToOKExAlgoOrderType(this PlaceOrderRequestParameters parameters)
        {
            return parameters.OrderTypeId switch
            {
                OrderType.Stop or OrderType.StopLimit => OKExAlgoOrderType.Conditional,
                OKExTriggerMarketOrderType.ID or OKExTriggerLimitOrderType.ID => OKExAlgoOrderType.Trigger,
                OKExOcoOrderType.ID => OKExAlgoOrderType.OCO,

                _ => throw new ArgumentException($"Unsupported order type - {parameters.OrderTypeId}"),
            };
        }
        public static bool IsModified(this OKExPosition p1, OKExPosition p2)
        {
            if (p1 == null || p2 == null)
                return false;

            return p1.AveragePrice != p2.AveragePrice || p1.Quantity != p2.Quantity;
        }

        public static string FormattedQuantity(this OKExSymbol symbol, double qty)
        {
            double newQty = qty;

            switch (symbol.InstrumentType)
            {
                case OKExInstrumentType.Swap:
                case OKExInstrumentType.Futures:
                case OKExInstrumentType.Option:
                    {
                        newQty = (double)((decimal)qty - (decimal)(qty % symbol.ContractMultiplier.Value));
                        break;
                    }
            }

            if (newQty < symbol.LotSize)
                newQty = symbol.LotSize.Value;

            return ((decimal)newQty).ToString(CultureInfo.InvariantCulture);
        }

        public static double ConvertSizeToBaseCurrency(this OKExSymbol symbol, OKExTradeItem tradeItem)
        {
            if (symbol.IsInverseContractSymbol)
                return ConvertSizeByContractValue(symbol.ContractValue.Value, tradeItem.Price.Value, tradeItem.Size.Value);
            else
                return tradeItem.Size.Value;
        }
        public static double ConvertSizeToBaseCurrency(this OKExSymbol symbol, OKExOrderBookItem item)
        {

            //return item.Size;

            if (symbol.IsInverseContractSymbol)
                return ConvertSizeByContractValue(symbol.ContractValue.Value, item.Price, item.Size);
            else
                return item.Size;
        }
        public static double ConvertSizeByContractValue(double contractValue, double price, double size) => size / price * contractValue;

        public static bool IsTopCurrency(this OKExSymbol symbol)
        {
            if (symbol.InstrumentType == OKExInstrumentType.Swap || symbol.InstrumentType == OKExInstrumentType.Futures)
            {
                return symbol.Underlier switch
                {
                    "BTC-USDT" or "ETH-USDT" or "LTC-USDT" or "ETC-USDT" or "XRP-USDT" or
                    "EOS-USDT" or "BCH-USDT" or "BSV-USDT" or "TRX-USDT" or "BTC-USD" or
                    "ETH-USD" or "LTC-USD" or "ETC-USD" or "XRP-USD" or "EOS-USD" or
                    "BCH-USD" or "BSV-USD" or "TRX-USD" => true,

                    _ => false,
                };
            }
            else if (symbol.InstrumentType == OKExInstrumentType.Spot)
            {
                return symbol.OKExInstrumentId switch
                {
                    "OKB-USDT" or "BTC-USDT" or "ETH-USDT" or "LTC-USDT" or "ETC-USDT" or
                    "XRP-USDT" or "EOS-USDT" or "BCH-USDT" or "BSV-USDT" or "TRX-USDT" => true,

                    _ => false,
                };
            }

            return false;
        }
        public static bool TryGetChannelName(this OKExSymbol symbol, OKExSubscriptionType subscriptionType, out string channelName)
        {
            channelName = null;

            switch (subscriptionType)
            {
                case OKExSubscriptionType.Last:
                    channelName = OKExChannels.TRADES;
                    break;
                case OKExSubscriptionType.Mark:
                    channelName = OKExChannels.MARK_PRICE;
                    break;
                case OKExSubscriptionType.Level2:
                    channelName = OKExChannels.ORDER_BOOK_400;
                    break;
                case OKExSubscriptionType.Ticker:
                    {
                        if (symbol.InstrumentType == OKExInstrumentType.Index)
                            channelName = OKExChannels.INDEX_TICKERS;
                        else
                            channelName = OKExChannels.TICKERS;
                        break;
                    }
                case OKExSubscriptionType.OpenInterest:
                    channelName = OKExChannels.OPEN_INTEREST;
                    break;
            }

            return channelName != null;

        }
        public static Period[] GetAvailablePeriods() => periodMapper.Select(s => s.Key).ToArray();


    }
}
