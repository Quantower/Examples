using System;
using System.Collections.Generic;
using System.Text;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace OKExV5Vendor.API.OrderType
{
    class OKExOcoOrderType : TradingPlatform.BusinessLayer.OrderType
    {
        internal const string ID = "OCO";
        public override string Id => ID;
        public override string Name => ID;

        public override OrderTypeBehavior Behavior => OrderTypeBehavior.Stop;

        public OKExOcoOrderType(params TimeInForce[] allowedTimeInForce)
            : base(allowedTimeInForce)
        {
            this.Usage = OrderTypeUsage.Order;
        }

        public override string GetCancelConfirmMessage(CancelOrderRequestParameters cancelRequest, FormatSettings formatSettings)
        {
            return string.Empty;
        }

        public override double GetFillPrice(OrderRequestParameters parameters)
        {
            return double.NaN;
        }

        public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings)
        {
            var settings = new List<SettingItem>();

            if (parameters.Symbol.SymbolType != SymbolType.Options)
            {
                OKExOrderTypeHelper.AddTradeMode(parameters, settings);
                OKExOrderTypeHelper.AddTakeProfit(parameters, settings);
                OKExOrderTypeHelper.AddStopLoss(parameters, settings);
            }

            if (parameters.Symbol.SymbolType == SymbolType.Crypto)
                OKExOrderTypeHelper.AddReduceOnly(settings);

            return settings;
        }

        protected override string GetModifyConfirmMessage(ModifyOrderRequestParameters modifyRequest, FormatSettings formatSettings)
        {
            return string.Empty;
        }

        protected override string GetPlaceConfirmMessage(PlaceOrderRequestParameters placeRequest, FormatSettings formatSettings)
        {
            var tpTriggerPrice = placeRequest.AdditionalParameters.GetVisibleValue<double>(OKExOrderTypeHelper.TAKE_PROFIT_TRIGGER_PRICE);
            var slTriggerPrice = placeRequest.AdditionalParameters.GetVisibleValue<double>(OKExOrderTypeHelper.STOP_LOSS_TRIGGER_PRICE);

            return $"{placeRequest.Side} {this.Name} {placeRequest.Symbol.FormatQuantity(placeRequest.Quantity, formatSettings.DisplayQuantityInLots)} {placeRequest.Symbol} at { placeRequest.Symbol.FormatPrice(tpTriggerPrice)} { placeRequest.Symbol.FormatPrice(slTriggerPrice)} " +
                $" for {placeRequest.Account}";
        }

        public override ValidateResult ValidateOrderRequestParameters(OrderRequestParameters parameters)
        {
            var result = base.ValidateOrderRequestParameters(parameters);

            if (result.State == ValidateState.NotValid)
                return result;

            var lastPrice = parameters.Symbol.Last;

            var tpTriggerPrice = parameters.AdditionalParameters.GetVisibleValue<double>(OKExOrderTypeHelper.TAKE_PROFIT_TRIGGER_PRICE);
            var slTriggerPrice = parameters.AdditionalParameters.GetVisibleValue<double>(OKExOrderTypeHelper.STOP_LOSS_TRIGGER_PRICE);

            if (parameters.Side == Side.Buy)
            {
                if (tpTriggerPrice >= lastPrice)
                    return ValidateResult.NotValid("Take profit trigger price should be lower than the last traded price.");

                if (slTriggerPrice <= lastPrice)
                    return ValidateResult.NotValid("Stop loss trigger price should be higher than the last traded price.");
            }
            else if (parameters.Side == Side.Sell)
            {
                if (tpTriggerPrice <= lastPrice)
                    return ValidateResult.NotValid("Take profit trigger price should be higher than the last traded price.");

                if (slTriggerPrice >= lastPrice)
                    return ValidateResult.NotValid("Stop loss trigger price should be lower than the last traded price.");
            }

            return ValidateResult.Valid;
        }

        
    }
}
