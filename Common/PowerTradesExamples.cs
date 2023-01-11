// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.History.Aggregations;
using TradingPlatform.BusinessLayer.PowerTrades;

namespace ApiExamples
{
    internal class PowerTradesExamples
    {
        /// <summary>
        /// https://help.quantower.com/quantower/analytics-panels/chart/power-trades
        /// </summary>
        public void Example()
        {
            // check if any symbol available
            if (Core.Instance.Symbols.Length == 0)
                return;

            // get first symbol from cache
            var symbol = Core.Instance.Symbols.FirstOrDefault();

            // create CancellationTokenSource instance
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // from 10 min before
            var fromTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddMinutes(-10);

            // to now (and process real-time trades)
            var toTime = default(DateTime);

            // define PowerTrades parameters
            var parameters = new HistoryAggregationPowerTradesParameters()
            {
                TotalVolume = 100,
                MinTradeVolume = 0,
                MaxTradeVolume = 100000,
                TimeInterval = 5,
                BasisVolumeInterval = 300,
                MinZoneHeight = 0,
                MaxZoneHeight = 100000,
                DeltaFilter = 50,
                BasisRatioFilter = 0,
            };

            // 1. Download ticks history
            // 2. Calculate 'PowerTrades' items (according to given parameters)
            // 3. Store them into 'HistoricalData' instance
            var powerTradesHistoricalData = symbol.GetHistory(new HistoryRequestParameters()
            {
                Aggregation = new HistoryAggregationPowerTrades(parameters),
                FromTime = fromTime,
                ToTime = toTime,
                Symbol = symbol,
                CancellationToken = cts.Token,
                HistoryType = HistoryType.Last,
                Period = Period.TICK1,
            });

            // process historical 'PowerTrades' items
            for (int i = 0; i < powerTradesHistoricalData.Count; i++)
            {
                var historyItem = powerTradesHistoricalData[i, SeekOriginHistory.Begin];
                this.ProcessHistoryItem(historyItem);
            }

            // subscribe to real-time updates (we want to receive new items)
            powerTradesHistoricalData.NewHistoryItem += this.HistoricalData_NewHistoryItem;
        }

        /// <summary>
        /// 'NewHistoryItem' handler
        /// </summary>
        private void HistoricalData_NewHistoryItem(object sender, HistoryEventArgs e)
        {
            // process real-time item
            this.ProcessHistoryItem(e.HistoryItem);
        }

        /// <summary>
        /// To do (something useful)
        /// </summary>
        private void ProcessHistoryItem(IHistoryItem item)
        {
            // cast base 'IHistoryItem' instance to needed 'IPowerTradesHistoryItem' interface
            if (item is not IPowerTradesHistoryItem powerTradesItem)
                return;

            // build log message by using 'power trades' item
            var message = $"Volome :{powerTradesItem.Cumulative} " +
                          $"Delta, perc: {powerTradesItem.DeltaPercent} " +
                          $"Basis ratio, perc: {powerTradesItem.BasisRatioPercent}";

            // log message
            Core.Instance.Loggers.Log(message, LoggingLevel.System);
        }
    }
}
