// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Trend
{
    /// <summary>
    /// The Average Directional Index (ADX) determines the strength of a prevailing trend.
    /// </summary>
    public class ADX : Indicator
    {
        #region Parameters

        // Displays Input Parameter as input field (or checkbox if value type is bolean).
        [InputParameter("Period", 0, 1, 999, 1, 0)]
        public int Period = 13;

        // Displays Input Parameter as dropdown list.
        [InputParameter("Type of Moving Average", 1, variants: new object[] {
            "Simple", MaMode.SMA,
            "Exponential", MaMode.EMA,
            "Modified", MaMode.SMMA,
            "Linear Weighted", MaMode.LWMA}
        )]
        public MaMode MAType = MaMode.SMA;

        #endregion

        private HistoricalDataCustom customHDadx;
        private HistoricalDataCustom customHDplusDm;
        private HistoricalDataCustom customHDminusDm;

        private Indicator rawAtr;
        private Indicator adxMa;
        private Indicator plusMa;
        private Indicator minusMa;

        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public ADX()
            : base()
        {
            // Defines indicator's group, name and description.            
            Name = "Average Directional Index";
            Description = "The ADX determines the strength of a prevailing trend.";

            // Defines line on demand with particular parameters.
            AddLineSeries("ADX'Line", Color.Green, 1, LineStyle.Solid);
            AddLineSeries("+DI'Line", Color.Blue, 1, LineStyle.Solid);
            AddLineSeries("-DI'Line", Color.Red, 1, LineStyle.Solid);

            SeparateWindow = true;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Serves for an identification of related indicators with different parameters.
            ShortName = "ADX (" + Period.ToString() + ": " + MAType.ToString() + ")";

            // Creates an instances of the custom historical data which will be syncronized by the current indicator instance.
            customHDadx = new HistoricalDataCustom(this);
            customHDplusDm = new HistoricalDataCustom(this);
            customHDminusDm = new HistoricalDataCustom(this);

            // Creates a indicators which will keep custom data.
            adxMa = Core.Indicators.BuiltIn.MA(Period, PriceType.Close, MAType);
            plusMa = Core.Indicators.BuiltIn.MA(Period, PriceType.Close, MAType);
            minusMa = Core.Indicators.BuiltIn.MA(Period, PriceType.Close, MAType);

            // Adds indicators to the custom historical data.
            customHDadx.AddIndicator(adxMa);
            customHDplusDm.AddIndicator(plusMa);
            customHDminusDm.AddIndicator(minusMa);

            // Creates an instance of the proper indicator from the default indicators list.
            rawAtr = Core.Indicators.BuiltIn.ATR(1, MAType);

            // Adds an auxiliary (ATR) indicator to the current one (ADX). 
            // This will let inner indicator (ATR) to be calculated in advance to the current one (ADX).
            AddIndicator(rawAtr);
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
            // Gets directional movement values.
            double plusDM, minusDM;
            GetPlusMinus(out plusDM, out minusDM);

            // Populates custom HistoricalData prices with the respective directional movement values.
            customHDplusDm[PriceType.Close] = plusDM;
            customHDminusDm[PriceType.Close] = minusDM;

            // Skip if count is smaller than period value.
            if (Count <= Period)
                return;

            // Gets smoothed directional movement values.
            double plusDI = plusMa.GetValue();
            double minusDI = minusMa.GetValue();

            // Calculates ADX.
            double adx = (plusDI != -minusDI) ? 100 * Math.Abs(plusDI - minusDI) / (plusDI + minusDI) : 0D;

            // Populates custom HistoricalData close price with the ADX value.
            customHDadx[PriceType.Close] = adx;

            // Sets values for displaying on the chart.
            SetValue(adxMa.GetValue());
            SetValue(plusDI, 1);
            SetValue(minusDI, 2);
        }

        /// <summary>
        /// Calculates directional movement of the same momentum.
        /// </summary>
        /// <param name="plusDM">positive directional movement</param>
        /// <param name="minusDM">negative directional movement</param>
        private void GetPlusMinus(out double plusDM, out double minusDM)
        {
            double rawATR = rawAtr.GetValue();

            if (Count < 2 || rawATR == 0)
            {
                plusDM = 0D;
                minusDM = 0D;
                return;
            }

            // Calculation of directional movement (DMs)
            plusDM = High() - High(1);
            if (plusDM < 0.0)
                plusDM = 0.0;
            else
                plusDM *= 100D / rawATR;
            minusDM = Low(1) - Low();
            if (minusDM < 0.0)
                minusDM = 0.0;
            else
                minusDM *= 100D / rawATR;

            if (plusDM > minusDM)
                minusDM = 0.0;
            else
                plusDM = 0.0;
        }
    }
}
