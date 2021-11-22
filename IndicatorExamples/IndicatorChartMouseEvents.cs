// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace IndicatorChartMouseEvents
{
	public class IndicatorChartMouseEvents : Indicator
    {
        private Point lastMouseDownPoint = Point.Empty;

        public IndicatorChartMouseEvents()
            : base()
        {
            // Defines indicator's name and description.
            Name = "IndicatorChartMouseEvents";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Subscribe to mouse events
            this.CurrentChart.MouseDown += CurrentChart_MouseDown;
            this.CurrentChart.MouseUp += CurrentChart_MouseUp;

            this.lastMouseDownPoint = Point.Empty;
        }

        private void CurrentChart_MouseDown(object sender, TradingPlatform.BusinessLayer.Chart.ChartMouseNativeEventArgs e)
        {
            if (e.Button == TradingPlatform.BusinessLayer.Native.NativeMouseButtons.Left &&
                this.CurrentChart.MainWindow.ClientRectangle.Contains(e.Location))
            {
                this.lastMouseDownPoint = e.Location;
                e.Handled = true;               
            }
        }

        private void CurrentChart_MouseUp(object sender, TradingPlatform.BusinessLayer.Chart.ChartMouseNativeEventArgs e)
        {
            this.lastMouseDownPoint = Point.Empty;
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            // First point not specified
            if (this.lastMouseDownPoint == Point.Empty)
                return;

            // Draw line between point where we clicked mouse and current mouse position
            Graphics gr = args.Graphics;
            Pen pen = new Pen(Brushes.Orange, 2);
            gr.DrawLine(pen, lastMouseDownPoint, args.MousePosition);
            gr.FillEllipse(Brushes.Orange, lastMouseDownPoint.X - 3, lastMouseDownPoint.Y - 3, 6, 6);
            gr.FillEllipse(Brushes.Orange, args.MousePosition.X - 3, args.MousePosition.Y - 3, 6, 6);

            // Calculate prices from Y coordinate (using CoordinatesConverter)
            double price1 = this.CurrentChart.MainWindow.CoordinatesConverter.GetPrice(lastMouseDownPoint.Y);
            string formattedPrice1 = this.Symbol.FormatPrice(price1);

            double price2 = this.CurrentChart.MainWindow.CoordinatesConverter.GetPrice(args.MousePosition.Y);
            string formattedPrice2 = this.Symbol.FormatPrice(price2);

            double deltaPrice = price2 - price1;
            string formattedDeltaPrice = this.Symbol.FormatPrice(deltaPrice);

            // Draw required text labels (using absolute coordinates and mouse positions)
            Font font = new Font("Arial", 8, FontStyle.Regular);
            gr.DrawString($"Price 1: {formattedPrice1}", font, Brushes.Orange, args.MousePosition.X + 20, args.MousePosition.Y);
            gr.DrawString($"Price 2: {formattedPrice2}", font, Brushes.Orange, args.MousePosition.X + 20, args.MousePosition.Y + 20);
            gr.DrawString($"Delta price: {formattedDeltaPrice}", font, Brushes.Orange, args.MousePosition.X + 20, args.MousePosition.Y + 40);
        }

        public override void Dispose()
        {
            // Unsubscribe from mouse events
            if (this.CurrentChart != null)
            {
                this.CurrentChart.MouseDown -= CurrentChart_MouseDown;
                this.CurrentChart.MouseUp -= CurrentChart_MouseUp;
            }

            base.Dispose();
        }

        protected override void OnUpdate(UpdateArgs args)
        { }
    }
}
