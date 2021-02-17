// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace MovingAverages
{
    public class SMA : Indicator
    {
        #region Parameters
        // Displays Input Parameter as input field.
        [InputParameter("Period of Simple Moving Average", 0, 1, 999, 1, 1)]
        public int Period = 10;

        // Displays Input Parameter as dropdown list.
        [InputParameter("Sources prices for MA", 1, variants: new object[]{
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical,
            "Median", PriceType.Median,
            "Weighted", PriceType.Weighted
        })]
        public PriceType SourcePrice = PriceType.Close;
        #endregion Parameters

        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public SMA()
            : base()
        {
            // Defines indicator's name and description.
            Name = "Simple Moving Average";
            Description = "Average price for the last N periods";

            // Define one line with particular parameters 
            AddLineSeries("SMA", Color.Red, 1, LineStyle.Solid);

            // 
            SeparateWindow = false;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Serves for an identification of related indicators with different parameters.
            ShortName = "SMA (" + Period + ":" + SourcePrice.ToString() + ")";
        }

        /// <summary>
        /// Calculation entry point. This function is called when a price data updates. 
        /// Will be runing under the HistoricalBar mode during history loading. 
        /// Under NewTick during realtime. 
        /// Under NewBar if start of the new bar is required.
        /// </summary>
        /// <param name="args">Provides data of updating reason and incoming price.</param>
        protected override void OnUpdate(UpdateArgs args)
        {
            // Checking, if current amount of bars
            // more, than period of moving average. If it is
            // then the calculation is possible
            if (Count <= Period)
                return;

            double sum = 0.0; // Sum of prices
            for (int i = 0; i < Period; i++)
                // Adding bar's price to the sum
                sum += GetPrice(SourcePrice, i);

            // Set value to the "SMA" line buffer.
            SetValue(sum / Period);
        }
    }
}
