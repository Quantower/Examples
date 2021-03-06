using System;
using System.Collections.Generic;
using System.Text;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API.OrderType
{
    static class OKExOrderTypeHelper
    {
        internal const string TAKE_PROFIT_TYPE = "TakeProfitType";
        internal const string TAKE_PROFIT_TRIGGER_PRICE = "TakeProfitTriggerPrice";
        internal const string TAKE_PROFIT_PRICE = "TakeProfitPrice";

        internal const string STOP_LOSS_TYPE = "StopLossType";
        internal const string STOP_LOSS_TRIGGER_PRICE = "StopLossTriggerPrice";
        internal const string STOP_LOSS_PRICE = "StopLossPrice";

        internal const string REDUCE_ONLY = "ReduceOnly";
        internal const string POST_ONLY = "PostOnly";

        internal const string TRADE_MODE_TYPE = "TradeModeType";
        internal const string MARGIN_CURRENCY = "MarginCurrency";

        internal static void AddTakeProfit(OrderRequestParameters parameters, IList<SettingItem> settings, OKExSLTPType type = OKExSLTPType.Market, int index = 0)
        {
            settings.Add(new SettingItemSelectorLocalized(TAKE_PROFIT_TYPE, new SelectItem("", (int)type), new List<SelectItem>()
            {
                new SelectItem(loc._("Market"), (int)OKExSLTPType.Market),
                new SelectItem(loc._("Limit"), (int)OKExSLTPType.Limit),
            })
            { Text = loc._("Take profit type"), SortIndex = index });

            var triggerPriceSI = CreatePriceBasedSettingItem(parameters, TAKE_PROFIT_TRIGGER_PRICE, parameters.Symbol.Last, index);
            triggerPriceSI.Text = loc._("TP trigger price");
            triggerPriceSI.Relation = new SettingItemRelationVisibility(TAKE_PROFIT_TYPE, new SelectItem("Market", (int)OKExSLTPType.Market), new SelectItem("Limit", (int)OKExSLTPType.Limit));

            var priceSI = CreatePriceBasedSettingItem(parameters, TAKE_PROFIT_PRICE, parameters.Symbol.Last, index);
            priceSI.Text = loc._("TP price");
            priceSI.Relation = new SettingItemRelationVisibility(TAKE_PROFIT_TYPE, new SelectItem("Limit", (int)OKExSLTPType.Limit));

            settings.Add(triggerPriceSI);
            settings.Add(priceSI);
        }
        internal static void AddStopLoss(OrderRequestParameters parameters, IList<SettingItem> settings, OKExSLTPType type = OKExSLTPType.Market, int index = 0)
        {
            settings.Add(new SettingItemSelectorLocalized(STOP_LOSS_TYPE, new SelectItem("", (int)type), new List<SelectItem>()
            {
                new SelectItem(loc._("Market"), (int)OKExSLTPType.Market),
                new SelectItem(loc._("Limit"), (int)OKExSLTPType.Limit),
            })
            { Text = loc._("Stop loss type"), SortIndex = index });

            var triggerPriceSI = CreatePriceBasedSettingItem(parameters, STOP_LOSS_TRIGGER_PRICE, parameters.Symbol.Last, index);
            triggerPriceSI.Text = loc._("SL trigger price");
            triggerPriceSI.Relation = new SettingItemRelationVisibility(STOP_LOSS_TYPE, new SelectItem("Market", (int)OKExSLTPType.Market), new SelectItem("Limit", (int)OKExSLTPType.Limit));

            var priceSI = CreatePriceBasedSettingItem(parameters, STOP_LOSS_PRICE, parameters.Symbol.Last, index);
            priceSI.Text = loc._("SL price");
            priceSI.Relation = new SettingItemRelationVisibility(STOP_LOSS_TYPE, new SelectItem("Limit", (int)OKExSLTPType.Limit));

            settings.Add(triggerPriceSI);
            settings.Add(priceSI);
        }
        internal static void AddReduceOnly(IList<SettingItem> settings, bool defaultValue = false, int index = 0)
        {
            settings.Add(new SettingItemBoolean(REDUCE_ONLY, defaultValue, index)
            {
                Text = loc._("Reduce only"),
                Relation = new SettingItemRelationVisibility(TRADE_MODE_TYPE, new SelectItem(loc._("Cross"), (int)OKExTradeMode.Cross), new SelectItem(loc._("Isolated"), (int)OKExTradeMode.Isolated))
            });
        }
        internal static void AddTradeMode(OrderRequestParameters parameters, IList<SettingItem> settings, OKExTradeMode tradeMode = OKExTradeMode.Cash, int index = 0)
        {
            settings.Add(new SettingItemSelectorLocalized(TRADE_MODE_TYPE, new SelectItem("", (int)tradeMode), new List<SelectItem>()
            {
                new SelectItem(loc._("Cash"), (int)OKExTradeMode.Cash),
                new SelectItem(loc._("Cross"), (int)OKExTradeMode.Cross),
                new SelectItem(loc._("Isolated"), (int)OKExTradeMode.Isolated),
            })
            { Text = loc._("Trade mode"), SortIndex = index });

            settings.Add(new SettingItemSelector(MARGIN_CURRENCY, parameters.Symbol.Product.Id, new List<string>()
            {
                parameters.Symbol.Product.Id,
                parameters.Symbol.QuotingCurrency.Id
            }, index)
            {
                Text = loc._("Margin currency"),
                Relation = new SettingItemRelationVisibility(TRADE_MODE_TYPE, new SelectItem(loc._("Cross"), (int)OKExTradeMode.Cross))
            });
        }
        internal static void AddPostOnly(IList<SettingItem> settings, bool defaultValue = false, int index = 0)
        {
            settings.Add(new SettingItemBoolean(POST_ONLY, defaultValue, index)
            {
                Text = loc._("Post only"),
            });
        }

        private static SettingItem CreatePriceBasedSettingItem(OrderRequestParameters parameters, string name, double value, int index)
        {
            if (double.IsNaN(value))
                value = 0;

            double increment = 1;
            int decimalPlaces = 0;

            var variableTick = parameters.Symbol.FindVariableTick(0);

            if (variableTick != null)
            {
                increment = variableTick.TickSize;
                decimalPlaces = variableTick.Precision;
            }

            return new SettingItemDouble(name, value, index)
            {
                Minimum = 0,
                Maximum = double.MaxValue,
                Increment = increment,
                DecimalPlaces = decimalPlaces,
            };
        }
    }
}
