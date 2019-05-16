using System;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class HistoryExamples
    {
        public void Example()
        {
            // 1. Get history
            
            // First you need to get the symbol
            Symbol symbol = Core.Instance.Symbols.FirstOrDefault();

            // Load 1-minute time aggregation for 1 day
            HistoricalData historicalData = symbol.GetHistory(Period.MIN1, DateTime.Now.AddDays(-1));

            // Load bid/ask ticks history for 1 hour
            historicalData = symbol.GetHistory(Period.TICK1, HistoryType.BidAsk, DateTime.Now.AddHours(-1));

            // Load 1-minute lasts history for 1 day, and aggregate it with renko algorithm.
            historicalData = symbol.GetHistory(new HistoryAggregationRenko(Period.MIN1, 10, RenkoStyle.AdvancedClassic, 100, 100, true, true), HistoryType.Last, DateTime.Now.AddDays(-1));

            // Load ticks history, and aggregate it with points and figures algorithm.
            // If HistoryType is omitted - will be used default history type for symbol.
            // If ToTime specified - historical data will not updated on real-time quotes.
            // ForceReload means that history will be loaded directly from server, local cache will be ignored 
            historicalData = symbol.GetHistory(new HistoryRequestParameters
            {
                Aggregation = new HistoryAggregationPointsAndFigures(Period.TICK1, 100, 50, PointsAndFiguresStyle.HighLow),
                FromTime = DateTime.Now.AddDays(-2),
                ToTime = DateTime.Now.AddDays(-1),
                ForceReload = true
            });
            
            
            // 2. Events
            
            // Will occur when current bar was updated by new quote
            historicalData.HistoryItemUpdated += this.HistoricalDataOnHistoryItemUpdated;
            
            // Will occur when new history bar was created
            historicalData.NewHistoryItem += this.HistoricalDataOnNewHistoryItem;
            
            
            // 3. Access to histoical data

            // You can receive any history item by using indexer
            
            // Get current history item
            IHistoryItem historyItem = historicalData[0];

            // Get previous history item
            historyItem = historicalData[1];

            // Get item with offset = 10
            historyItem = historicalData[10];
            
            // By default, the countdown is going from right to left (from latest time to long time)
            // But, you can change this behavior by using SeekOriginHistory enum
            
            // Get history item at index = 3.
            historyItem = historicalData[3, SeekOriginHistory.Begin];
            
            // When you get history item, you can read its data
            DateTime leftTime = historyItem.TimeLeft;             // Allowed for all history types
            double open = historyItem[PriceType.Open];            // Allowed for bar history only
            double high = historyItem[PriceType.High];            // Allowed for bar history only
            double low = historyItem[PriceType.Low];              // Allowed for bar history only
            double close = historyItem[PriceType.Close];          // Allowed for bar history only
            double median = historyItem[PriceType.Median];        // Allowed for bar history only
            double typical = historyItem[PriceType.Typical];      // Allowed for bar history only
            double weighted = historyItem[PriceType.Weighted];    // Allowed for bar history only
            double ticks = historyItem[PriceType.Ticks];          // Allowed for bar history only
            double volume = historyItem[PriceType.Volume];        // Allowed for bar history and ticks history with HistoryType.Last

            double bid = historyItem[PriceType.Bid];              // Allowed for ticks history with HistoryType.BidAsk
            double bidSize = historyItem[PriceType.BidSize];      // Allowed for ticks history with HistoryType.BidAsk
            double ask = historyItem[PriceType.Ask];              // Allowed for ticks history with HistoryType.BidAsk
            double askSize = historyItem[PriceType.AskSize];      // Allowed for ticks history with HistoryType.BidAsk

            double last = historyItem[PriceType.Last];            // Allowed for ticks history with HistoryType.Last

            // Alternative way is cast variable to its real type
            // If you know, that history is bar history, you can do this:
            HistoryItemBar barItem = historyItem as HistoryItemBar;
            if (barItem != null)
            {
                leftTime = barItem.TimeLeft;
                open = barItem.Open;
                high = barItem.High;
                low = barItem.Low;
                close = barItem.Close;
                median = barItem.Median;
                typical = barItem.Typical;
                weighted = barItem.Weighted;
                ticks = barItem.Ticks;
                volume = barItem.Volume;
            }
            
            // For bid/ask ticks history
            HistoryItemTick tickItem = historyItem as HistoryItemTick;
            if (tickItem != null)
            {
                bid = tickItem.Bid;
                bidSize = tickItem.BidSize;
                ask = tickItem.Ask;
                askSize = tickItem.AskSize;
            }
            
            // For last ticks history
            HistoryItemLast lastItem = historyItem as HistoryItemLast;
            if (lastItem != null)
            {
                last = lastItem.Price;
                volume = lastItem.Volume;
            }
        }

        private void HistoricalDataOnHistoryItemUpdated(object sender, HistoryEventArgs e)
        {
            IHistoryItem historyItem = e.HistoryItem;
            
            // do something with item
        }
        
        private void HistoricalDataOnNewHistoryItem(object sender, HistoryEventArgs e)
        {
            IHistoryItem historyItem = e.HistoryItem;

            // do something with item
        }
    }
}