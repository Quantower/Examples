// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Volatility
{
    public class CMO : Indicator
    {
        #region Parameters
        // Displays Input Parameter as input field.
        [InputParameter("Period of MA for envelopes", 0, 1, 999, 1, 0)]
        public int Period = 8;

        // Displays Input Parameter as dropdown list.
        [InputParameter("Sources prices for MA", 1, variants: new object[] {
             "Close", PriceType.Close,
             "Open", PriceType.Open,
             "High", PriceType.High,
             "Low", PriceType.Low,
             "Typical", PriceType.Typical,
             "Medium", PriceType.Median,
             "Weighted", PriceType.Weighted}
        )]
        public PriceType SourcePrice = PriceType.Close;
        #endregion

        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public CMO()
            : base()
        {
            // Defines indicator's name and description.
            Name = "Chande Momentum Oscillator";
            Description = "Calculates the dividing of difference between the sum of all recent gains and the sum of all recent losses by the sum of all price movement over the period.";

            // Defines line on demand with particular parameters.
            AddLineSeries("CMO", Color.Purple, 2, LineStyle.Solid);
            AddLineLevel(50, "Up", Color.Blue, 1, LineStyle.Dash);
            AddLineLevel(-50, "Down", Color.Blue, 1, LineStyle.Dash);
            SeparateWindow = true;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Serves for an identification of related indicators with different parameters.
            ShortName = "CMO (" + Period + ":" + SourcePrice.ToString() + ")";
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
            // Skip some period for correct calculation.  
            if (Count <= Period)
                return;

            // Calculate the sum 
            double sum1 = 0d;
            double sum2 = 0d;
            for (int i = 0; i < Period; i++)
            {
                double diff = GetPrice(SourcePrice, i) - GetPrice(SourcePrice, i + 1);
                if (diff > 0)
                    sum1 += diff;
                else
                    sum2 -= diff;
            }

            // Compute the cmo value and set its to the 'CMO' line. 
            SetValue(100.0 * ((sum1 - sum2) / (sum1 + sum2)));
        }
    }
}
