// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace ApiExamples
{
    public class SubscribeMarketDataExamples
    {
        public static void Example()
        {
            // Get first available symbol
            Symbol symbol = Core.Instance.Symbols.FirstOrDefault();

            // Or you can find symbol by name
            symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "EUR/USD");

            // Subscribe to realtime market data updates
            if (symbol != null)
            {
                symbol.NewQuote += Symbol_NewQuote;
                symbol.NewLevel2 += Symbol_NewLevel2;
                symbol.NewLast += Symbol_NewLast;
                symbol.NewDayBar += Symbol_NewDayBar;
            }
        }

        private static void Symbol_NewQuote(Symbol symbol, Quote quote)
        {
            // Get data from quote
            double bid = quote.Bid;
            double bidSize = quote.BidSize;

            double ask = quote.Ask;
            double askSize = quote.AskSize;

            // Or get data from symbol object
            bid = symbol.Bid;
            bidSize = symbol.BidSize;

            ask = symbol.Ask;
            askSize = symbol.AskSize;
        }

        private static void Symbol_NewLevel2(Symbol symbol, Level2Quote level2, DOMQuote dom)
        {
            // Snapshot level2 update
            if (dom != null)
            {
                List<Level2Quote> bids = dom.Bids;
                List<Level2Quote> asks = dom.Asks;
            }

            // Incremental level2 update
            if (level2 != null)
            {
                QuotePriceType priceType = level2.PriceType; // Bid or Ask
                double price = level2.Price;
                double size = level2.Size;

                if (level2.Closed)
                {
                    // If level is closed and should be deleted
                }
            }

            // Get market depth info from symbol
            var depthOfMarket = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();
        }

        private static void Symbol_NewLast(Symbol symbol, Last last)
        {
            // Get data from last
            double lastPrice = last.Price;
            double lastSize = last.Size;

            // Or get data from symbol object
            lastPrice = symbol.Last;
            lastSize = symbol.LastSize;
        }

        private static void Symbol_NewDayBar(Symbol symbol, DayBar dayBar)
        {
            // Get data from day bar
            double open = dayBar.Open;
            double high = dayBar.High;
            double low = dayBar.Low;
            double prevClose = dayBar.PreviousClose;

            // Or get data from symbol object
            open = symbol.Open;
            high = symbol.High;
            low = symbol.Low;
            prevClose = symbol.PrevClose;
        }
    }
}
