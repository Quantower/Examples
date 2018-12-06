using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    public class SimpleMACross : Strategy
    {
        [InputParameter("Symbol", 0)]
        public Symbol symbol;

        [InputParameter("Account", 1)]
        public Account account;

        [InputParameter("Period", 5)]
        private Period period = Period.MIN5;

        [InputParameter("Fast MA", 2, minimum: 1, maximum: 100, increment: 1, decimalPlaces: 0)]
        public int fastMA = 5;

        [InputParameter("Slow MA", 3, minimum: 1, maximum: 100, increment: 1, decimalPlaces: 0)]
        public int slowMA = 10;

        [InputParameter("Quantity", 4, 0.1, 99999, 0.1, 2)]
        public double quantity = 1.0;

        public Indicator indicatorFastMA;
        public Indicator indicatorSlowMA;

        public HistoricalData hdm;
        private int longPositionsCount;
        private int shortPositionsCount;

        public SimpleMACross()
            : base()
        {
            this.Name = "Simple MA Cross strategy";
            this.Description = "Raw strategy without any additional functional";
        }

        protected override void OnCreated()
        {
            base.OnCreated();
        }

        protected override void OnRun()
        {
            if (symbol == null || account == null || symbol.ConnectionId != account.ConnectionId)
            {
                Log("Incorrect input parameters... Symbol or Account are not specified or they have diffent connectionID.", StrategyLoggingLevel.Error);
                return;
            }

            this.symbol.NewQuote += this.QuoteHandler;

            this.indicatorFastMA = Core.Instance.Indicators.BuiltIn.SMA(this.fastMA, PriceType.Close);
            this.indicatorSlowMA = Core.Instance.Indicators.BuiltIn.SMA(this.slowMA, PriceType.Close);

            this.hdm = this.symbol.GetHistory(period, this.symbol.HistoryType, DateTime.UtcNow.AddDays(-100));

            this.hdm.AddIndicator(this.indicatorFastMA);
            this.hdm.AddIndicator(this.indicatorSlowMA);
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (this.symbol != null)
            {
                this.symbol.NewQuote -= this.QuoteHandler;
            }

        }

        protected override void OnRemove()
        {
            base.OnRemove();
        }

        protected override List<StrategyMetric> OnGetMetrics()
        {
            List<StrategyMetric> result = base.OnGetMetrics();

            // An example of adding custom strategy metrics:
            result.Add("Total long positions", longPositionsCount.ToString());
            result.Add("Total short positions", shortPositionsCount.ToString());

            return result;
        }

        private void QuoteHandler(Symbol instrument, Quote quote)
        {
            if (Core.Instance.Positions.Length != 0)
            {
                //Закрытие позиций
                if (this.indicatorFastMA.GetValue(1) < this.indicatorSlowMA.GetValue(1) || this.indicatorFastMA.GetValue(1) > this.indicatorSlowMA.GetValue(1))
                {
                    TradingOperationResult result = Core.Instance.ClosePosition(new ClosePositionRequestParameters()
                    {
                        Position = Core.Instance.Positions[0],
                        CloseQuantity = Core.Instance.Positions[0].Quantity
                    });

                    if (result.Status == TradingOperationResultStatus.Success)
                        Log($"{result.Status}. Position was closed.", StrategyLoggingLevel.Trading);
                }
            }

            if (Core.Instance.Positions.Length == 0)
            {
                //Открытие новых позиций
                if (this.indicatorFastMA.GetValue(1) > this.indicatorSlowMA.GetValue(1))
                {
                    TradingOperationResult result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters()
                    {
                        OrderTypeId = OrderType.Market,
                        Quantity = quantity,
                        Side = Side.Buy,
                        Account = this.account,
                        Symbol = this.symbol
                    });
                    if (result.Status == TradingOperationResultStatus.Success)
                        Log($"{result.Status}. Long position was placed.", StrategyLoggingLevel.Trading);

                    longPositionsCount++;
                }

                else if (this.indicatorFastMA.GetValue(1) < this.indicatorSlowMA.GetValue(1))
                {
                    TradingOperationResult result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters()
                    {
                        OrderTypeId = OrderType.Market,
                        Quantity = quantity,
                        Side = Side.Sell,
                        Account = this.account,
                        Symbol = this.symbol
                    });
                    if (result.Status == TradingOperationResultStatus.Success)
                        Log($"{result.Status}. Short position was placed.", StrategyLoggingLevel.Trading);

                    shortPositionsCount++;
                }
            }
        }
    }
}