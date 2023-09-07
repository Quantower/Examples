// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    public sealed class SimpleMACross : Strategy, ICurrentAccount, ICurrentSymbol
    {
        [InputParameter("Symbol", 0)]
        public Symbol CurrentSymbol { get; set; }

        /// <summary>
        /// Account to place orders
        /// </summary>
        [InputParameter("Account", 1)]
        public Account CurrentAccount { get; set; }

        /// <summary>
        /// Period for Fast MA indicator
        /// </summary>
        [InputParameter("Fast MA", 2, minimum: 1, maximum: 100, increment: 1, decimalPlaces: 0)]
        public int FastMA { get; set; }

        /// <summary>
        /// Period for Slow MA indicator
        /// </summary>
        [InputParameter("Slow MA", 3, minimum: 1, maximum: 100, increment: 1, decimalPlaces: 0)]
        public int SlowMA { get; set; }

        /// <summary>
        /// Quantity to open order
        /// </summary>
        [InputParameter("Quantity", 4, 0.1, 99999, 0.1, 2)]
        public double Quantity { get; set; }

        /// <summary>
        /// Period to load history
        /// </summary>
        [InputParameter("Period", 5)]
        public Period Period { get; set; }

        /// <summary>
        /// Start point to load history
        /// </summary>
        [InputParameter("Start point", 6)]
        public DateTime StartPoint { get; set; }

        public override string[] MonitoringConnectionsIds => new string[] { this.CurrentSymbol?.ConnectionId, this.CurrentAccount?.ConnectionId };

        private Indicator indicatorFastMA;
        private Indicator indicatorSlowMA;

        private HistoricalData hdm;

        private int longPositionsCount;
        private int shortPositionsCount;
        private string orderTypeId;

        private bool waitOpenPosition;
        private bool waitClosePositions;

        private double totalNetPl;
        private double totalGrossPl;
        private double totalFee;

        public SimpleMACross()
            : base()
        {
            this.Name = "Simple MA Cross strategy";
            this.Description = "Raw strategy without any additional functional";

            this.FastMA = 5;
            this.SlowMA = 10;
            this.Period = Period.MIN5;
            this.StartPoint = Core.TimeUtils.DateTimeUtcNow.AddDays(-100);
        }

        protected override void OnRun()
        {
            this.totalNetPl = 0D;

            // Restore symbol object from active connection
            if (this.CurrentSymbol != null && this.CurrentSymbol.State == BusinessObjectState.Fake)
                this.CurrentSymbol = Core.Instance.GetSymbol(this.CurrentSymbol.CreateInfo());

            if (this.CurrentSymbol == null)
            {
                this.Log("Incorrect input parameters... Symbol have not specified.", StrategyLoggingLevel.Error);
                return;
            }

            // Restore account object from active connection
            if (this.CurrentAccount != null && this.CurrentAccount.State == BusinessObjectState.Fake)
                this.CurrentAccount = Core.Instance.GetAccount(this.CurrentAccount.CreateInfo());

            if (this.CurrentAccount == null)
            {
                this.Log("Incorrect input parameters... Account have not specified.", StrategyLoggingLevel.Error);
                return;
            }

            if (this.CurrentSymbol.ConnectionId != this.CurrentAccount.ConnectionId)
            {
                this.Log("Incorrect input parameters... Symbol and Account from different connections.", StrategyLoggingLevel.Error);
                return;
            }

            this.orderTypeId = Core.OrderTypes.FirstOrDefault(x => x.ConnectionId == this.CurrentSymbol.ConnectionId && x.Behavior == OrderTypeBehavior.Market).Id;

            if (string.IsNullOrEmpty(this.orderTypeId))
            {
                this.Log("Connection of selected symbol has not support market orders", StrategyLoggingLevel.Error);
                return;
            }

            this.indicatorFastMA = Core.Instance.Indicators.BuiltIn.SMA(this.FastMA, PriceType.Close);
            this.indicatorSlowMA = Core.Instance.Indicators.BuiltIn.SMA(this.SlowMA, PriceType.Close);

            this.hdm = this.CurrentSymbol.GetHistory(this.Period, this.CurrentSymbol.HistoryType, this.StartPoint);

            Core.PositionAdded += this.Core_PositionAdded;
            Core.PositionRemoved += this.Core_PositionRemoved;

            Core.OrdersHistoryAdded += this.Core_OrdersHistoryAdded;

            Core.TradeAdded += this.Core_TradeAdded;

            this.hdm.HistoryItemUpdated += this.Hdm_HistoryItemUpdated;

            this.hdm.AddIndicator(this.indicatorFastMA);
            this.hdm.AddIndicator(this.indicatorSlowMA);
        }

        protected override void OnStop()
        {
            Core.PositionAdded -= this.Core_PositionAdded;
            Core.PositionRemoved -= this.Core_PositionRemoved;

            Core.OrdersHistoryAdded -= this.Core_OrdersHistoryAdded;

            Core.TradeAdded -= this.Core_TradeAdded;

            if (this.hdm != null)
            {
                this.hdm.HistoryItemUpdated -= this.Hdm_HistoryItemUpdated;
                this.hdm.Dispose();
            }

            base.OnStop();
        }

        protected override void OnInitializeMetrics(Meter meter)
        {
            base.OnInitializeMetrics(meter);

            meter.CreateObservableCounter("total-long-positions", () => this.longPositionsCount, description: "Total long positions");
            meter.CreateObservableCounter("total-short-positions", () => this.shortPositionsCount, description: "Total short positions");

            meter.CreateObservableCounter("total-pl-net", () => this.totalNetPl, description: "Total Net profit/loss");
            meter.CreateObservableCounter("total-pl-gross", () => this.totalGrossPl, description: "Total Gross profit/loss");
            meter.CreateObservableCounter("total-fee", () => this.totalFee, description: "Total fee");
        }

        private void Core_PositionAdded(Position obj)
        {
            var positions = Core.Instance.Positions.Where(x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount).ToArray();
            this.longPositionsCount = positions.Count(x => x.Side == Side.Buy);
            this.shortPositionsCount = positions.Count(x => x.Side == Side.Sell);

            double currentPositionsQty = positions.Sum(x => x.Side == Side.Buy ? x.Quantity : -x.Quantity);

            if (Math.Abs(currentPositionsQty) == this.Quantity)
                this.waitOpenPosition = false;
        }

        private void Core_PositionRemoved(Position obj)
        {
            var positions = Core.Instance.Positions.Where(x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount).ToArray();
            this.longPositionsCount = positions.Count(x => x.Side == Side.Buy);
            this.shortPositionsCount = positions.Count(x => x.Side == Side.Sell);

            if (!positions.Any())
                this.waitClosePositions = false;
        }

        private void Core_OrdersHistoryAdded(OrderHistory obj)
        {
            if (obj.Symbol == this.CurrentSymbol)
                return;

            if (obj.Account == this.CurrentAccount)
                return;

            if (obj.Status == OrderStatus.Refused)
                this.ProcessTradingRefuse();
        }

        private void Core_TradeAdded(Trade obj)
        {
            if (obj.NetPnl != null)
                this.totalNetPl += obj.NetPnl.Value;

            if (obj.GrossPnl != null)
                this.totalGrossPl += obj.GrossPnl.Value;

            if (obj.Fee != null)
                this.totalFee += obj.Fee.Value;
        }

        private void Hdm_HistoryItemUpdated(object sender, HistoryEventArgs e) => this.OnUpdate();

        private void OnUpdate()
        {
            var positions = Core.Instance.Positions.Where(x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount).ToArray();

            if (this.waitOpenPosition)
                return;

            if (this.waitClosePositions)
                return;

            if (positions.Any())
            {
                //Закрытие позиций
                if (this.indicatorFastMA.GetValue(1) < this.indicatorSlowMA.GetValue(1) || this.indicatorFastMA.GetValue(1) > this.indicatorSlowMA.GetValue(1))
                {
                    this.waitClosePositions = true;
                    this.Log($"Start close positions ({positions.Length})");

                    foreach (var item in positions)
                    {
                        var result = item.Close();

                        if (result.Status == TradingOperationResultStatus.Failure)
                        {
                            this.Log($"Close positions refuse: {(string.IsNullOrEmpty(result.Message) ? result.Status : result.Message)}", StrategyLoggingLevel.Trading);
                            this.ProcessTradingRefuse();
                        }
                        else
                            this.Log($"Position was close: {result.Status}", StrategyLoggingLevel.Trading);
                    }
                }
            }
            else
            {
                //Открытие новых позиций
                if (this.indicatorFastMA.GetValue(2) < this.indicatorSlowMA.GetValue(2) && this.indicatorFastMA.GetValue(1) > this.indicatorSlowMA.GetValue(1))
                {
                    this.waitOpenPosition = true;
                    this.Log("Start open buy position");
                    var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters()
                    {
                        Account = this.CurrentAccount,
                        Symbol = this.CurrentSymbol,

                        OrderTypeId = this.orderTypeId,
                        Quantity = this.Quantity,
                        Side = Side.Buy,
                    });

                    if (result.Status == TradingOperationResultStatus.Failure)
                    {
                        this.Log($"Place buy order refuse: {(string.IsNullOrEmpty(result.Message) ? result.Status : result.Message)}", StrategyLoggingLevel.Trading);
                        this.ProcessTradingRefuse();
                    }
                    else
                        this.Log($"Position open: {result.Status}", StrategyLoggingLevel.Trading);
                }
                else if (this.indicatorFastMA.GetValue(2) > this.indicatorSlowMA.GetValue(2) && this.indicatorFastMA.GetValue(1) < this.indicatorSlowMA.GetValue(1))
                {
                    this.waitOpenPosition = true;
                    this.Log("Start open sell position");
                    var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters()
                    {
                        Account = this.CurrentAccount,
                        Symbol = this.CurrentSymbol,

                        OrderTypeId = this.orderTypeId,
                        Quantity = this.Quantity,
                        Side = Side.Sell,
                    });

                    if (result.Status == TradingOperationResultStatus.Failure)
                    {
                        this.Log($"Place sell order refuse: {(string.IsNullOrEmpty(result.Message) ? result.Status : result.Message)}", StrategyLoggingLevel.Trading);
                        this.ProcessTradingRefuse();
                    }
                    else
                        this.Log($"Position open: {result.Status}", StrategyLoggingLevel.Trading);
                }
            }
        }

        private void ProcessTradingRefuse()
        {
            this.Log("Strategy have received refuse for trading action. It should be stopped", StrategyLoggingLevel.Error);
            this.Stop();
        }
    }
}
