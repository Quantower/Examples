// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Oscillators
{
    public class AO : Indicator
    {
        private Indicator fastMA;
        private Indicator slowMA;

        // Fixed periods for SMA indicators.
        private const int FAST_PERIOD = 5;
        private const int SLOW_PERIOD = 34;

        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public AO()
            : base()
        {
            // Defines indicator's name and description.
            Name = "Awesome Oscillator";
            Description = "Awesome Oscillator determines market momentum";

            // Defines line on demand with particular parameters.
            AddLineSeries("AO", Color.Gray, 2, LineStyle.Histogramm);
            SeparateWindow = true;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Serves for an identification of related indicators with different parameters.
            ShortName = "AO";

            // Get two SMA indicators from built-in indicator collection.
            fastMA = Core.Indicators.BuiltIn.SMA(FAST_PERIOD, PriceType.Median);
            slowMA = Core.Indicators.BuiltIn.SMA(SLOW_PERIOD, PriceType.Median);

            AddIndicator(fastMA);
            AddIndicator(slowMA);
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
            // Skip max period for correct calculation. 
            if (Count <= SLOW_PERIOD)
                return;

            // Calculate the AO value
            double ao = fastMA.GetValue() - slowMA.GetValue();
            double prevAO = double.IsNaN(GetValue(1))
                ? GetPrice(PriceType.Close, 0)
                : GetValue(1);

            // Set values to 'AO' line buffer.
            SetValue(ao);

            Color indicatorColor = (prevAO > ao)
                ? Color.Red
                : Color.Green;

            LinesSeries[0].SetMarker(1, indicatorColor);
        }
    }
}
