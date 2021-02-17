// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace MovingAverages
{
    /// <summary>
    /// The SMMA gives recent prices an equal weighting to historic prices.
    /// </summary>
    public class SMMA : Indicator
    {
        #region Parameters

        // Period of moving average. 
        [InputParameter("Period of Smoothed Moving Average", 0, 1, 999, 1, 0)]
        public int MaPeriod = 2;

        // Price type of moving average. 
        [InputParameter("Sources prices for MA", 1, variants: new object[]
        {
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical,
            "Median", PriceType.Median,
            "Weighted", PriceType.Weighted
        })]
        public PriceType SourcePrice = PriceType.Close;

        // Calculation coefficient.
        private double k;

        #endregion

        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public SMMA()
            : base()
        {
            // Defines indicator's group, name and description.            
            Name = "Smoothed Moving Average";
            Description = "The SMMA gives recent prices an equal weighting to historic prices.";

            // Defines line on demand with particular parameters.
            AddLineSeries("SMMA", Color.DodgerBlue, 1, LineStyle.Solid);

            SeparateWindow = false;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Serves for an identification of related indicators with different parameters.
            ShortName = "SMMA (" + MaPeriod.ToString() + ": " + SourcePrice.ToString() + ")";
            // Calculation of a coefficient.
            k = 2.0 / (MaPeriod + 1);
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
            // Checking, if current amount of bars less, than period of moving average - calculation is impossible.
            if (Count <= MaPeriod)
                return;

            // Calculating the current value of the indicator.
            double value = 0;
            if (double.IsNaN(GetValue(1)))
            {
                // Calculates initial value as Simple Moving Average.
                double summa = 0.0;
                for (int i = 0; i < MaPeriod; i++)
                    summa += this.GetPrice(SourcePrice, i);
                value = summa / MaPeriod;
            }
            else
            {
                value = (GetValue(1) * (MaPeriod - 1) + GetPrice(SourcePrice)) / MaPeriod;
            }

            // Sets value for displaying on the chart.
            SetValue(value);
        }
    }
}
