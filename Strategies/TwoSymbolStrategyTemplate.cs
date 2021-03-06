// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace TwoSymbolStrategy
{
    /// <summary>
    /// An example of strategy for working with one symbol. Add your code, compile it and run via Strategy Runner panel in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// </summary>
	public class TwoSymbolStrategy : Strategy
    {
        // Two accounts are required if you are trading two products with different denominations. IE) USD account for US stocks and AUD account for ASX stocks.
        [InputParameter("Symbol One", 10)]
        private Symbol symbolOne;

        [InputParameter("Account One", 20)]
        public Account accountOne;

        [InputParameter("Symbol Two", 30)]
        private Symbol symbolTwo;

        [InputParameter("Account One", 40)]
        public Account accountTwo;

        public override string[] MonitoringConnectionsIds => new string[] { this.symbolOne?.ConnectionId, this.symbolTwo?.ConnectionId };

        public TwoSymbolStrategy()
            : base()
        {
            // Defines strategy's name and description.
            this.Name = "TwoSymbolStrategy";
            this.Description = "My strategy's annotation";
        }

        /// <summary>
        /// This function will be called after creating a strategy
        /// </summary>
        protected override void OnCreated()
        {
            // Add your code here
        }

        /// <summary>
        /// This function will be called after running a strategy
        /// </summary>
        protected override void OnRun()
        {
            if (symbolOne == null || accountOne == null || symbolOne.ConnectionId != accountOne.ConnectionId)
            {
                Log("Symbol One - Incorrect input parameters... Symbol or Account are not specified or they have diffent connectionID.", StrategyLoggingLevel.Error);
                return;
            }

            if (symbolTwo == null || accountTwo == null || symbolOne.ConnectionId != accountTwo.ConnectionId)
            {
                Log("Symbol Two - Incorrect input parameters... Symbol or Account are not specified or they have diffent connectionID.", StrategyLoggingLevel.Error);
                return;
            }

            this.symbolOne = Core.GetSymbol(this.symbolOne?.CreateInfo());
            this.symbolTwo = Core.GetSymbol(this.symbolTwo?.CreateInfo());

            if (this.symbolOne != null)
            {
                this.symbolOne.NewQuote += SymbolOnNewQuote;
                this.symbolOne.NewLast += SymbolOnNewLast;
            }

            if (this.symbolTwo != null)
            {
                this.symbolTwo.NewQuote += SymbolOnNewQuote;
                this.symbolTwo.NewLast += SymbolOnNewLast;
            }

            // Add your code here
        }

        /// <summary>
        /// This function will be called after stopping a strategy
        /// </summary>
        protected override void OnStop()
        {
            if (this.symbolOne != null)
            {
                this.symbolOne.NewQuote -= SymbolOnNewQuote;
                this.symbolOne.NewLast -= SymbolOnNewLast;
            }
            if (this.symbolTwo != null)
            {
                this.symbolTwo.NewQuote -= SymbolOnNewQuote;
                this.symbolTwo.NewLast -= SymbolOnNewLast;
            }

            // Add your code here
        }

        /// <summary>
        /// This function will be called after removing a strategy
        /// </summary>
        protected override void OnRemove()
        {
            this.symbolOne = null;
            this.accountOne = null;
            this.symbolTwo = null;
            this.accountTwo = null;
            // Add your code here
        }

        /// <summary>
        /// Use this method to provide run time information about your strategy. You will see it in StrategyRunner panel in trading terminal
        /// </summary>
        protected override List<StrategyMetric> OnGetMetrics()
        {
            List<StrategyMetric> result = base.OnGetMetrics();

            // An example of adding custom strategy metrics:
            // result.Add("Opened buy orders", "2");
            // result.Add("Opened sell orders", "7");

            return result;
        }

        private void SymbolOnNewQuote(Symbol symbol, Quote quote)
        {
            // Add your code here
        }

        private void SymbolOnNewLast(Symbol symbol, Last last)
        {
            // Add your code here
        }
    }
}
