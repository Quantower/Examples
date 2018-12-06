using System;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace Strategies
{
    /// <summary>
    /// 3MACrossAdvisor
    /// 3 MA Cross .NET
    /// </summary>
    public class ThreeMACrossAdvisor : Strategy
    {
        private const int BARS_THREE_MA_INTERVAL = 1;

        #region Parameters
        [InputParameter("Symbol", 10)]
        private Symbol symbol;

        [InputParameter("Account", 20)]
        public Account account;

        [InputParameter("Period", 20)]
        private Period period = Period.MIN1;

        [InputParameter("Short Moving Average Period", 30, 1, 100)]
        public int ShortMaPeriod = 5;

        [InputParameter("Middle Moving Average Period", 40, 1, 100)]
        public int MiddleMaPeriod = 10;

        [InputParameter("Long Moving Average Period", 50, 1, 100)]
        public int LongMaPeriod = 25;

        [InputParameter("Quantity", 60, 0.1, 100, 0.1, 2)]
        public double quantity = 1.0;

        #endregion Parameters

        private Indicator threeMaIndicator;
        private HistoricalData historicalData;
        private TradingOperationResult currentOrderResult;
        private TradingState tradingState;
        private int longOrdersCount;
        private int shortOrdersCount;
        private double maxPeriod;

        public ThreeMACrossAdvisor()
        {
            this.Name = "3MA Cross Advisor";
        }

        protected override void OnRun()
        {
            if (symbol == null || account == null || symbol.ConnectionId != account.ConnectionId)
            {
                Log("Incorrect input parameters... Symbol or Account are not specified or they have diffent connections.", StrategyLoggingLevel.Error);
                return;
            }

            threeMaIndicator = Core.Instance.Indicators.BuiltIn.MAS3(ShortMaPeriod, MiddleMaPeriod, LongMaPeriod, BARS_THREE_MA_INTERVAL);
            historicalData = symbol.GetHistory(period, symbol.HistoryType, DateTime.UtcNow);
            historicalData.AddIndicator(threeMaIndicator);

            tradingState = TradingState.ExitMarket;
            longOrdersCount = 0;
            shortOrdersCount = 0;
            maxPeriod = Enumerable.Max(new double[] { ShortMaPeriod, MiddleMaPeriod, LongMaPeriod });
            symbol.NewQuote += OnNewQuote;
            symbol.NewLast += OnNewLast;
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (symbol != null)
            {
                symbol.NewQuote -= OnNewQuote;
                symbol.NewLast -= OnNewLast;
            }
        }

        protected override List<StrategyMetric> OnGetMetrics()
        {
            var metrics = base.OnGetMetrics();
            metrics.Add(loc._("Total long positions: "), longOrdersCount.ToString());
            metrics.Add(loc._("Total short positions: "), shortOrdersCount.ToString());

            return metrics;
        }

        private void OnNewLast(Symbol symbol, Last last)
        {
            StrategyProcess(last);
        }

        private void OnNewQuote(Symbol symbol, Quote quote)
        {
            StrategyProcess(quote);
        }

        private void StrategyProcess(MessageQuote message)
        {
            if (historicalData.Count <= maxPeriod)
                return;

            // Calculation of trend 
            Trend trend = (Trend)threeMaIndicator.GetValue();

            switch (trend)
            {
                case Trend.Up:
                    // Up trend detected. If we were in short position, first closing it
                    if (tradingState == TradingState.EnteredSell)
                    {
                        // If request for closing has been sent, setting the current state - 
                        // we have already exit the market
                        Position position = Core.Instance.GetPositionById(currentOrderResult.OrderId, symbol.ConnectionId);
                        if (position == null)
                            return;

                        var result = position.Close();
                        if (result.Status == TradingOperationResultStatus.Success)
                        {
                            tradingState = TradingState.ExitMarket;
                            Log($"{currentOrderResult.Status}. Position was closed.", StrategyLoggingLevel.Trading);
                        }

                        // exitting the program to give some time to
                        // the system for processing the order. Entrance will 
                        // be performed on the next quote
                        return;
                    }

                    // If we haven't aleady entered the market, do it 
                    if (tradingState != TradingState.EnteredBuy)
                    {
                        var orderPrice = symbol.Bid;
                        if (message is Last last)
                            orderPrice = last.Price;

                        if (message is Quote quote)
                            orderPrice = quote.Ask;

                        // Sending request for opening long position, and 
                        // setting the state - "Entered long position"
                        currentOrderResult = Core.PlaceOrder(new PlaceOrderRequestParameters
                        {
                            Account = account,
                            Symbol = this.symbol,
                            Price = orderPrice,
                            OrderTypeId = OrderType.Market,
                            Quantity = quantity,
                            Side = Side.Buy,
                        });
                        longOrdersCount++;
                        tradingState = TradingState.EnteredBuy;

                        if (currentOrderResult.Status == TradingOperationResultStatus.Success)
                            Log($"{currentOrderResult.Status}. Long position was placed.", StrategyLoggingLevel.Trading);
                        else
                            Log($"{currentOrderResult.Status}. {currentOrderResult.Message}", StrategyLoggingLevel.Trading);
                    }
                    break;

                case Trend.Down:
                    //Down trend detected. If we were in long position, firstly closing it 
                    if (tradingState == TradingState.EnteredBuy)
                    {
                        // If request for closing has been sent, setting the current state - 
                        // we have already exit the market
                        Position position = Core.Instance.GetPositionById(currentOrderResult.OrderId, symbol.ConnectionId);
                        if (position == null)
                            return;

                        var result = position.Close();
                        if (result.Status == TradingOperationResultStatus.Success)
                        {
                            tradingState = TradingState.ExitMarket;
                            Log($"{currentOrderResult.Status}. Position was closed.", StrategyLoggingLevel.Trading);
                        }
                        // exitting the program to give some time to
                        // the system for processing the order. Entrance will 
                        // be performed on the next quote
                        return;
                    }

                    // If we haven't aleady entered the market, do it  
                    if (tradingState != TradingState.EnteredSell)
                    {
                        var orderPrice = symbol.Bid;
                        if (message is Last last)
                            orderPrice = last.Price;

                        if (message is Quote quote)
                            orderPrice = quote.Bid;

                        // Sending request for opening long position, and 
                        // if request is sent, then setting the state - "Entered short position"
                        currentOrderResult = Core.PlaceOrder(new PlaceOrderRequestParameters
                        {
                            Account = account,
                            Symbol = this.symbol,
                            Price = orderPrice,
                            OrderTypeId = OrderType.Market,
                            Quantity = quantity,
                            Side = Side.Sell,
                        });
                        shortOrdersCount++;
                        tradingState = TradingState.EnteredSell;

                        if(currentOrderResult.Status == TradingOperationResultStatus.Success)
                            Log($"{currentOrderResult.Status}. Short position was placed.", StrategyLoggingLevel.Trading);
                        else
                            Log($"{currentOrderResult.Status}. {currentOrderResult.Message}", StrategyLoggingLevel.Trading);
                    }
                    break;
            }
        }

        internal enum Trend
        {
            /// <summary>
            /// Constant of BUY trade signal
            /// </summary>
            Up = 1,

            /// <summary>
            /// Constant of NO_TREND trade signal
            /// </summary>
            No = 0,

            /// <summary>
            /// Constant of SELL SHORT trade signal
            /// </summary>
            Down = -1,
        }

        internal enum TradingState
        {
            /// <summary>
            /// ATS entered long position
            /// </summary>
            EnteredBuy = 1,

            /// <summary>
            /// ATS entered short position
            /// </summary>
            EnteredSell = -1,

            /// <summary>
            /// ATS exit the market
            /// </summary>
            ExitMarket = 0,
        }
    }
}
