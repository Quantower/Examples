// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Bitfinex.API.Models;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor.Extensions;

internal static class OrderSettingsExtensions
{
    public static IList<SettingItem> AddReduceOnly(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type != RequestType.PlaceOrder)
            return settings;

        if (parameters.Account.Id.StartsWith(BitfinexWalletType.MARGIN))
        {
            settings.Add(new SettingItemBoolean(OrderType.REDUCE_ONLY, false, sortIndex)
            {
                Text = loc._("Reduce-only")
            });
        }

        return settings;
    }

    public static bool IsReduceOnly(this IList<SettingItem> settings) => settings.GetValueOrDefault(false, OrderType.REDUCE_ONLY);

    public static IList<SettingItem> AddPostOnly(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type != RequestType.PlaceOrder)
            return settings;

        settings.Add(new SettingItemBoolean(OrderType.POST_ONLY, false, sortIndex)
        {
            Text = loc._("Post-only")
        });

        return settings;
    }

    public static bool IsPostOnly(this IList<SettingItem> settings) => settings.GetValueOrDefault(false, OrderType.POST_ONLY);

    public static IList<SettingItem> AddHidden(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        settings.Add(new SettingItemBoolean(BitfinexVendor.HIDDEN, false, sortIndex)
        {
            Text = loc._("Hidden")
        });

        return settings;
    }

    public static bool IsHidden(this IList<SettingItem> settings) => settings.GetValueOrDefault(false, BitfinexVendor.HIDDEN);

    public static IList<SettingItem> AddOco(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type != RequestType.PlaceOrder)
            return settings;

        settings.Add(new SettingItemBoolean(BitfinexVendor.OCO, false, sortIndex)
        {
            Text = loc._("OCO")
        });

        return settings;
    }

    public static bool IsOco(this IList<SettingItem> settings) => settings.GetValueOrDefault(false, BitfinexVendor.OCO);

    public static IList<SettingItem> AddOcoStopPrice(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type != RequestType.PlaceOrder)
            return settings;

        double price = 0;
        double minChange = 1E-5;
        int precision = 5;

        var symbol = parameters.Symbol;

        if (symbol != null)
        {
            if (parameters.Type == RequestType.PlaceOrder)
            {
                price = parameters.Side == Side.Buy ? symbol.Bid : symbol.Ask;

                price = CoreMath.ProcessNaN(price);
            }

            var variableTick = symbol.FindVariableTick(price);

            minChange = variableTick.TickSize;
            precision = variableTick.Precision;
        }

        settings.Add(new SettingItemDouble(BitfinexVendor.OCO_STOP_PRICE, price)
        {
            Text = loc._("OCO Stop price"),
            SortIndex = sortIndex,
            Minimum = minChange,
            Maximum = int.MaxValue,
            Increment = minChange,
            DecimalPlaces = precision,
            Relation = new SettingItemRelationVisibility(BitfinexVendor.OCO, true)
        });

        return settings;
    }

    public static double GetOcoStopPrice(this IList<SettingItem> settings) => settings.GetValueOrDefault(0d, BitfinexVendor.OCO_STOP_PRICE);

    public static IList<SettingItem> AddLeverage(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Symbol.SymbolType == SymbolType.Swap)
        {
            settings.Add(new SettingItemInteger(BitfinexVendor.LEVERAGE, 10, sortIndex)
            {
                Text = loc._("Leverage"),
                Minimum = 1,
                Maximum = BitfinexVendor.MAX_LEVERAGE,
                Increment = 1
            });
        }

        return settings;
    }

    public static int GetLeverage(this IList<SettingItem> settings) => settings.GetValueOrDefault(10, BitfinexVendor.LEVERAGE);

    public static IList<SettingItem> AddClientOrderId(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type == RequestType.ModifyOrder)
            return settings;

        long.TryParse(parameters.Comment, out long value);

        settings.Add(new SettingItemLong(BitfinexVendor.CLIENT_ORDER_ID, value, sortIndex)
        {
            Text = loc._("Comment"),
            Minimum = 0
        });

        return settings;
    }

    public static long? GetClientOrderId(this IList<SettingItem> settings)
    {
        long? result = settings.GetValueOrDefault<long?>(null, BitfinexVendor.CLIENT_ORDER_ID);

        return result is null or < 1 ? null : result;
    }
}