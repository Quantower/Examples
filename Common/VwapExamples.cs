// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Abstractions;
using TradingPlatform.BusinessLayer.History.Aggregations;

namespace ApiExamples;

/// <summary>
/// https://help.quantower.com/quantower/analytics-panels/chart/vwap
/// </summary>
class VwapExamples
{
    /// <summary>
    /// Like standard 'VWAP' tool. 
    /// </summary>
    internal void GetVwapByPeriodExample()
    {
        // check if any symbol available
        if (Core.Instance.Symbols.Length == 0)
            return;

        // get first symbol from cache
        var symbol = Core.Instance.Symbols.FirstOrDefault();

        // create CancellationTokenSource instance
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // from 1 Day before
        var fromTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(-1);

        // to now (and process real-time data)
        var toTime = default(DateTime);

        // define vwap parameters.
        // For example, we want to calculate 1H vwap on 5min aggregation.
        var parameters = new HistoryAggregationVwapParameters()
        {
            Aggregation = new HistoryAggregationTime(Period.MIN5),          // Aggregation. Like chart aggregation, where we want to display vwap values.
            DataType = VwapDataType.CurrentTF,                              // DataType. Use ticks or aggregated history to calculate vwap values
            Period = Period.HOUR1,                                          // VWAP period.
            PriceType = VwapPriceType.HLC3,                                 // PriceType. Only works with 'VwapDataType.CurrentTF' 
            StdCalculationType = VwapStdCalculationType.StandardDeviation,  // STD formula
            TimeZone = Core.Instance.TimeUtils.SelectedTimeZone,            // Use terminal timezone, by default
        };

        // 1. Download data history
        // 2. Calculate 'VWAP' items (according to given parameters)
        // 3. Store them into 'HistoricalData' instance
        var vwapHistoricalData = symbol.GetHistory(new HistoryRequestParameters()
        {
            Aggregation = new HistoryAggregationVwap(parameters),  // Create VWAP-based historical data
            Symbol = symbol,                                      
            FromTime = fromTime,                                   
            ToTime = toTime,
            CancellationToken = cts.Token,
            HistoryType = symbol.HistoryType,                      // Last, Mark, Bid, Ask etc. (Depends on data feed) 
            SessionsContainer = DefaultSessionsContainer.Instance, // Default session (it means full day).
        });

        // process historical 'VWAP' items
        for (int i = 0; i < vwapHistoricalData.Count; i++)
        {
            var historyItem = vwapHistoricalData[i, SeekOriginHistory.Begin];
            this.ProcessHistoryItem(historyItem, true);
        }

        vwapHistoricalData.NewHistoryItem += this.VwapHistoricalData_NewHistoryItem;
        vwapHistoricalData.HistoryItemUpdated += this.VwapHistoricalData_HistoryItemUpdated;
    }

    /// <summary>
    /// Like 'Custom VWAP' drawing tool. Calculate one full-range vwap line.
    /// </summary>
    internal void GetVwapByRangeExample()
    {
        // check if any symbol available
        if (Core.Instance.Symbols.Length == 0)
            return;

        // get first symbol from cache
        var symbol = Core.Instance.Symbols.FirstOrDefault();

        // create CancellationTokenSource instance
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // from 1-Day min before
        var fromTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(-1);

        // to now (and process real-time data)
        var toTime = default(DateTime);

        // define vwap parameters.
        // For example, we want to calculate full-range vwap on 5min aggregation.
        var parameters = new HistoryAggregationVwapParameters()
        {
            Aggregation = new HistoryAggregationTime(Period.MIN5),          // Aggregation. Like chart aggregation, where we want to display vwap values.
            DataType = VwapDataType.CurrentTF,                              // DataType. Use ticks or aggregated history to calculate vwap values
            PriceType = VwapPriceType.HLC3,                                 // PriceType. Only works with 'VwapDataType.CurrentTF' 
            StdCalculationType = VwapStdCalculationType.VWAPVariance,       // STD formula
        };

        // 1. Download data history
        // 2. Calculate 'VWAP' items (according to given parameters)
        // 3. Store them into 'HistoricalData' instance
        var vwapHistoricalData = symbol.GetHistory(new HistoryRequestParameters()
        {
            Aggregation = new HistoryAggregationVwap(parameters),  // Create VWAP-based historical data
            Symbol = symbol,
            FromTime = fromTime,
            ToTime = toTime,
            CancellationToken = cts.Token,
            HistoryType = symbol.HistoryType,                      // Last,Mark,Bid,Ask etc. (Depends on data feed) 
        });

        // process historical 'VWAP' items
        for (int i = 0; i < vwapHistoricalData.Count; i++)
        {
            var historyItem = vwapHistoricalData[i, SeekOriginHistory.Begin];
            this.ProcessHistoryItem(historyItem, true);
        }

        vwapHistoricalData.NewHistoryItem += this.VwapHistoricalData_NewHistoryItem;
        vwapHistoricalData.HistoryItemUpdated += this.VwapHistoricalData_HistoryItemUpdated;
    }

    private void VwapHistoricalData_HistoryItemUpdated(object sender, HistoryEventArgs e)
    {
        this.ProcessHistoryItem(e.HistoryItem, false);
    }
    private void VwapHistoricalData_NewHistoryItem(object sender, HistoryEventArgs e)
    {
        this.ProcessHistoryItem(e.HistoryItem, true);
    }

    private void ProcessHistoryItem(IHistoryItem historyItem, bool isNewItem)
    {
        if (historyItem is not IVwapHistoryItem vwap)
            return;

        // build log message by using 'vwap' item
        var message = $"Time :{vwap.TimeLeft} | " +
                      $"Value :{vwap.Value} | " +
                      $"STD coeff.: {vwap.STDCoefficient} | " +
                      $"MPD coeff.: {vwap.MPDCoefficient} | ";

        // log message
        Core.Instance.Loggers.Log(message, LoggingLevel.System);
    }
}
