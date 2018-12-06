using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace Strategies
{
    public class PDOExpert : Strategy
    {
        #region Parameters

        [InputParameter("Symbol", 10)]
        private Symbol symbol;

        [InputParameter("Account", 20)]
        public Account account;

        [InputParameter("Period", 30)]
        private Period period = Period.MIN1;

        [InputParameter("Price Type", 40, variants: new object[] {
            "Close", PriceType.Close,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Medium", PriceType.Median,
            "Open", PriceType.Open,
            "Typical", PriceType.Typical,
            "Weighted", PriceType.Weighted
        })]
        public PriceType priceType = PriceType.Close;

        //[InputParameter("Magic number", 50, 1, 9999)]
        //public int MagicNumber = 13;

        [InputParameter("Quantity", 60, 0.1, 99999, 0.1, 2)]
        public double Amount = 1.0;

        [InputParameter("Stop loss", 70, 1, 9999)]
        public int SL = 100;

        [InputParameter("Take profit", 80, 1, 9999)]
        public int TP = 100;

        [InputParameter("RSI Period", 90, 1, 9999)]
        public int RSIPeriod = 14;

        [InputParameter("SMA period", 100, 1, 9999)]
        public int SMAPeriod = 200;

        [InputParameter("Up Trend Entry", 110, 1, 9999)]
        public double upTrendEntry = 35;

        [InputParameter("Down Trend Exit", 120, 1, 9999)]
        public double downTrendExit = 70;

        [InputParameter("Stop offset", 130, 1, 1000)]
        public int stopOffset = 400;

        #endregion Parameters

        private TradingOperationResult _operationResult;
        private Indicator _maIndicator;
        private Indicator _rsiIndicator;
        private HistoricalData _historicalData;
        private int maxPeriod;
        private double _pdoValue;
        private double _prevPdoValue;
        private int longOrdersCount;
        private int shortOrdersCount;

        /// <summary>
        /// Strategy's constructor. Contains general information: name, description etc. 
        /// </summary>
        public PDOExpert()
            : base()
        {
            // Defines strategy's name and description.
            this.Name = "PDO Expert";
            this.Description = "My strategy's annotation";
        }

        /// <summary>
        /// This function will be called after creating a strategy
        /// </summary>
        protected override void OnCreated()
        {
            _maIndicator = Core.Indicators.BuiltIn.MA(SMAPeriod, priceType, MaMode.SMA);
            _rsiIndicator = Core.Indicators.BuiltIn.RSI(RSIPeriod, priceType, RSIMode.Simple, MaMode.SMA, 1);
        }

        /// <summary>
        /// This function will be called after running a strategy
        /// </summary>
        protected override void OnRun()
        {
            if (symbol == null || account == null || symbol.ConnectionId != account.ConnectionId)
            {
                Log("Incorrect input parameters... Symbol or Account are not specified or they have diffent connections.", StrategyLoggingLevel.Error);
                return;
            }

            var vendorName = Core.Connections.Connected.FirstOrDefault(c => c.Id == symbol.ConnectionId);
            var isLimitSupported = Core.GetOrderType(OrderType.Limit, symbol.ConnectionId) != null;

            if (!isLimitSupported && vendorName != null)
            {
                Log($"The '{vendorName}' doesn't support '{OrderType.Limit}' order type.", StrategyLoggingLevel.Error);
                return;
            }

            _historicalData = symbol.GetHistory(period, Core.TimeUtils.DateTimeUtcNow); // ???
            _historicalData.AddIndicator(_maIndicator);
            _historicalData.AddIndicator(_rsiIndicator);

            _historicalData.NewHistoryItem += this._historicalData_NewHistoryItem;
            _historicalData.HistoryItemUpdated += this._historicalData_HistoryItemUpdated;

            maxPeriod = new int[] { SMAPeriod, RSIPeriod }.Max();
        }

        private void _historicalData_HistoryItemUpdated(object sender, HistoryEventArgs e)
        {
            if (_historicalData.Count <= maxPeriod)
                return;

            _pdoValue = (_historicalData[0][priceType] > _maIndicator.GetValue())
                ? (_rsiIndicator.GetValue() - 35.0) / 50.0 * 100
                : (_rsiIndicator.GetValue() - 20.0) / 50.0 * 100;
        }

        private void _historicalData_NewHistoryItem(object sender, HistoryEventArgs e)
        {
            if (_historicalData.Count <= maxPeriod)
                return;

            StrategyProcess();
        }

        private void StrategyProcess()
        {
            if (_pdoValue > upTrendEntry && _prevPdoValue <= upTrendEntry)
                CreateLimitOrder(Side.Buy);
            else if (_pdoValue < downTrendExit && _prevPdoValue >= downTrendExit)
                CreateLimitOrder(Side.Sell);

            _prevPdoValue = _pdoValue;
        }

        private void CreateLimitOrder(Side side)
        {
            if (_operationResult != null)
            {
                if (Core.GetPositionById(_operationResult.OrderId, symbol.ConnectionId) != null)
                    return;

                var order = Core.Orders.FirstOrDefault(o => o.ConnectionId == symbol.ConnectionId && o.Id == _operationResult.OrderId);
                if (order != null)
                {
                    if (order.Side == Side.Buy)
                        longOrdersCount--;
                    else
                        shortOrdersCount--;

                    order.Cancel();
                    Log("Order was canceled.", StrategyLoggingLevel.Trading);
                }
            }

            var sign = (side == Side.Buy) ? -1 : 1;

            var orderPrice = (side == Side.Buy)
                ? symbol.CalculatePrice(symbol.Ask, stopOffset * sign)
                : symbol.CalculatePrice(symbol.Bid, stopOffset * sign);

            var slPrice = orderPrice + sign * SL * symbol.TickSize;
            var tpPrice = orderPrice - sign * TP * symbol.TickSize;

            _operationResult = Core.PlaceOrder(new PlaceOrderRequestParameters()
            {
                Account = account,
                Symbol = symbol,
                Side = side,
                OrderTypeId = OrderType.Limit,
                Price = orderPrice,
                StopLoss = SlTpHolder.CreateSL(slPrice),
                TakeProfit = SlTpHolder.CreateTP(tpPrice),
                Quantity = Amount
            });

            var formatedSide = string.Empty;
            if(side == Side.Buy)
            {
                formatedSide = "Long";
                longOrdersCount++;
            }
            else
            {
                formatedSide = "Short";
                shortOrdersCount++;
            }

            if (_operationResult.Status == TradingOperationResultStatus.Success)
                Log($"{_operationResult.Status}. {formatedSide} order was placed.", StrategyLoggingLevel.Trading);
        }

        /// <summary>
        /// This function will be called after stopping a strategy
        /// </summary>
        protected override void OnStop()
        {
            if (_historicalData == null)
                return;

            _historicalData.RemoveIndicator(_maIndicator);
            _historicalData.RemoveIndicator(_rsiIndicator);

            _historicalData.NewHistoryItem -= this._historicalData_NewHistoryItem;
            _historicalData.HistoryItemUpdated -= this._historicalData_HistoryItemUpdated;
        }

        /// <summary>
        /// This function will be called after removing a strategy
        /// </summary>
        protected override void OnRemove()
        {
            if (_historicalData != null)
                _historicalData.Dispose();
        }

        /// <summary>
        /// Use this method to provide run time information about your strategy. You will see it in StrategyRunner panel in trading terminal
        /// </summary>
        protected override List<StrategyMetric> OnGetMetrics()
        {
            List<StrategyMetric> result = base.OnGetMetrics();
          
            // An example of adding custom strategy metrics:
             result.Add("Opened buy orders", longOrdersCount.ToString());
             result.Add("Opened sell orders", shortOrdersCount.ToString());

            return result;
        }
    }
}
