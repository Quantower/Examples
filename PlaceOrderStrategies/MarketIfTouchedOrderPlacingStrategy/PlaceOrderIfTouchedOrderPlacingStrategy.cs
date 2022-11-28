// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.LocalOrders;
using TradingPlatform.BusinessLayer.Modules;
using TradingPlatform.BusinessLayer.Utils;

namespace PlaceOrderIfTouchedOrderPlacingStrategy
{
    public abstract class PlaceOrderIfTouchedOrderPlacingStrategy : OrderPlacingStrategy
    {
        #region Parameters

        private CancellationTokenSource cts;

        [InputParameter("HistoryType", 1, variants: new object[]{
            HistoryType.Last, HistoryType.Last,
            HistoryType.Bid, HistoryType.Bid,
            HistoryType.Ask, HistoryType.Ask,
        })]
        public HistoryType HistoryType;

        private PlaceOrderRequestParameters placeOrderRequest;
        private bool crossFromBelow;
        private bool finished;

        protected string ShortName { get; set; }

        #endregion Parameters

        public PlaceOrderIfTouchedOrderPlacingStrategy()
        {
            this.HistoryType = HistoryType.Last;
        }

        #region Overrides

        protected override void OnPlaceOrder(PlaceOrderRequestParameters placeOrderRequest)
        {
            string localOrderId = string.Empty;

            try
            {
                this.placeOrderRequest = placeOrderRequest;
                this.cts = new CancellationTokenSource();

                // Create local order
                var localOrder = new LocalOrder
                {
                    Symbol = placeOrderRequest.Symbol,
                    Account = placeOrderRequest.Account,
                    Side = placeOrderRequest.Side,
                    TotalQuantity = placeOrderRequest.Quantity,
                    OrderType = new CustomOrderType(this.ShortName, placeOrderRequest.OrderType),
                    TimeInForce = placeOrderRequest.TimeInForce,
                    Price = placeOrderRequest.Price,
                    TriggerPrice = placeOrderRequest.TriggerPrice,
                    TrailOffset = placeOrderRequest.TrailOffset
                };
                localOrderId = Core.Instance.LocalOrders.AddOrder(localOrder);
                Core.Instance.LocalOrders.Updated += LocalOrdersOnUpdated;

                if (this.HistoryType == HistoryType.Last)
                {
                    this.crossFromBelow = placeOrderRequest.Symbol.Last < placeOrderRequest.Price;

                    placeOrderRequest.Symbol.NewLast += this.Symbol_NewLast;
                }
                else
                {
                    this.crossFromBelow = placeOrderRequest.Symbol.Last < placeOrderRequest.Price;

                    placeOrderRequest.Symbol.NewQuote += this.Symbol_NewQuote;
                }

                while (!this.finished && !this.cts.IsCancellationRequested)
                    Thread.Sleep(100);
            }
            finally
            {
                Core.Instance.LocalOrders.Updated -= LocalOrdersOnUpdated;
                Core.Instance.LocalOrders.RemoveOrder(localOrderId);
            }

            void LocalOrdersOnUpdated(object sender, LocalOrderEventArgs e)
            {
                var localOrder = e.LocalOrder;

                if (localOrderId != localOrder.Id)
                    return;

                if (e.Lifecycle == EntityLifecycle.Removed)
                {
                    this.cts?.Cancel();
                    return;
                }

                placeOrderRequest.Price = localOrder.Price;
                placeOrderRequest.TriggerPrice = localOrder.TriggerPrice;
                placeOrderRequest.TrailOffset = localOrder.TrailOffset;
                placeOrderRequest.Quantity = localOrder.TotalQuantity;
                placeOrderRequest.TimeInForce = localOrder.TimeInForce;
                var placeOrderAdditionalParameters = placeOrderRequest.AdditionalParameters;
                placeOrderAdditionalParameters.UpdateValues(localOrder.AdditionalInfo);
                placeOrderRequest.AdditionalParameters = placeOrderAdditionalParameters;
            }
        }
        protected override void OnCancel() => this.cts?.Cancel();
        public override void Dispose()
        {
            if (placeOrderRequest != null && placeOrderRequest.Symbol != null)
            {
                if (this.HistoryType == HistoryType.Last)
                    placeOrderRequest.Symbol.NewLast -= this.Symbol_NewLast;
                else
                    placeOrderRequest.Symbol.NewQuote -= this.Symbol_NewQuote;
            }

            base.Dispose();
        }

        #endregion Overrides

        #region Event handlers

        private void Symbol_NewQuote(Symbol symbol, Quote quote) => this.ProcessPrice(this.HistoryType == HistoryType.Bid ? quote.Bid : quote.Ask);
        private void Symbol_NewLast(Symbol symbol, Last last) => this.ProcessPrice(last.Price);
        private void ProcessPrice(double price)
        {
            if (this.finished || this.cts.IsCancellationRequested)
                return;

            if (this.crossFromBelow && price >= this.placeOrderRequest.Price ||
                !this.crossFromBelow && price <= this.placeOrderRequest.Price)
            {
                try
                {
                    PlaceOrderRequestParameters marketRequest = (PlaceOrderRequestParameters)placeOrderRequest.Clone();
                    marketRequest.SendingSource = this.Name;

                    this.CustomizeOrderRequest(marketRequest);
                    Core.Instance.PlaceOrder(marketRequest);
                }
                finally
                {
                    this.finished = true;
                }
            }
        }

        #endregion Event handlers

        protected abstract void CustomizeOrderRequest(PlaceOrderRequestParameters placeOrderRequest);
    }
}
