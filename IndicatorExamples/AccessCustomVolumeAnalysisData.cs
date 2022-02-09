// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace AccessCustomVolumeAnalysisData
{
    /// <summary>
    /// Indicator displays volume analysis data for last HOURS_COUNT hours
    /// </summary>
	public class AccessCustomVolumeAnalysisData : Indicator
    {
        private HistoricalData hoursHistory;
        private const int HOURS_COUNT = 5;
        private IVolumeAnalysisCalculationProgress loadingVolumeAnalysisProgress;

        public AccessCustomVolumeAnalysisData()
            : base()
        {
            // Defines indicator's name and description.
            Name = "AccessCustomVolumeAnalysisData";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            // Request required history timeframe (independent from chart where indicator is running)
            this.hoursHistory = this.Symbol.GetHistory(Period.HOUR1, this.Symbol.HistoryType, DateTime.UtcNow.AddHours(-HOURS_COUNT * 2));
         
            // Request volume analysis data
            this.loadingVolumeAnalysisProgress = Core.Instance.VolumeAnalysis.CalculateProfile(this.hoursHistory);  
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            Font font = new Font("Arial", 8, FontStyle.Regular);

            // Check if everything is ready to draw volume analysis data
            if (this.hoursHistory == null || this.loadingVolumeAnalysisProgress == null || this.loadingVolumeAnalysisProgress.State != VolumeAnalysisCalculationState.Finished || this.hoursHistory.Count < HOURS_COUNT)
            {
                args.Graphics.DrawString($"Volume analysis data is loading {this.loadingVolumeAnalysisProgress.ProgressPercent}%...", font, Brushes.LightBlue, 30, 50);
                return;
            }

            // Draw volume analysis data
            args.Graphics.DrawString($"Last {HOURS_COUNT} hours volume:", font, Brushes.LightBlue, 30, 50);
            for (int i = 0; i < HOURS_COUNT; i++)
                args.Graphics.DrawString($"{this.hoursHistory[i].TimeLeft.ToShortTimeString()} = {this.Symbol.FormatQuantity(this.hoursHistory[i].VolumeAnalysisData.Total.Volume)}", font, Brushes.LightBlue, 30, 70 + 20 * i);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (this.loadingVolumeAnalysisProgress != null && this.loadingVolumeAnalysisProgress.State != VolumeAnalysisCalculationState.Finished)
                this.loadingVolumeAnalysisProgress.AbortLoading();
            this.hoursHistory?.Dispose();
        } 
    }
}
