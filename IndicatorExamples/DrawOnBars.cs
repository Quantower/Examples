// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace DrawOnBars
{
    /// <summary>
    /// An example of indicator that uses GDI to draw custom text for each bar
    /// </summary>
	public class DrawOnBars : Indicator
    {
        public DrawOnBars()
            : base()
        {
            // Defines indicator's name and description.
            Name = "DrawOnBars";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }

        protected override void OnInit() { }

        protected override void OnUpdate(UpdateArgs args) { }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null)
                return;

            // Use args.Hdc to create Graphics which give us acces to chart canvas
            Graphics graphics = Graphics.FromHdc(args.Hdc);

            // Use StringFormat class to center text
            StringFormat stringFormat = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            var mainWindow = this.CurrentChart.MainWindow;
            Font font = new Font("Arial", 10, FontStyle.Bold);

            // Get left and right time from visible part or history
            DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
            DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);

            // Convert left and right time to index of bar
            int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
            int rightIndex = (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(rightTime));

            // Process only required (visible on the screen at the moment) range of bars
            for (int i = leftIndex; i <= rightIndex; i++)
            {
                if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar)
                {
                    bool isBarGrowing = bar.Close > bar.Open;

                    // Calculate coordinates for drawing text. X - middle of the bar. Y - above High or below Low
                    int textXCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar.TimeLeft) + this.CurrentChart.BarsWidth / 2.0);
                    int textYCoord = (int)Math.Round(isBarGrowing ? (mainWindow.CoordinatesConverter.GetChartY(bar.High) - 20) : (mainWindow.CoordinatesConverter.GetChartY(bar.Low) + 20));

                    graphics.DrawString(isBarGrowing ? "Up" : "Down", font, isBarGrowing ? Brushes.Green : Brushes.Red, textXCoord, textYCoord, stringFormat);
                }
            }
        }
    }
}
