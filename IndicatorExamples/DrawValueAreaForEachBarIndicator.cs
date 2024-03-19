// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace DrawValueAreaForEachBarIndicator
{
    public class DrawValueAreaForEachBarIndicator : Indicator, IVolumeAnalysisIndicator
    {
        private bool volumeAnalysisLoaded;

        [InputParameter("Value area", minimum: 0, maximum: 100)]
        public int ValueAreaPercent { get; set; } = 70;

        public DrawValueAreaForEachBarIndicator()
            : base()
        {
            // Defines indicator's name and description.
            Name = "DrawValueAreaForEachBarIndicator";
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);
            SeparateWindow = false;
        }

        public bool IsRequirePriceLevelsCalculation => true;

        public void VolumeAnalysisData_Loaded()
            => this.volumeAnalysisLoaded = true;

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (!this.volumeAnalysisLoaded)
                return;

            var mainWindow = this.CurrentChart.MainWindow;
            Graphics gr = args.Graphics;

            var prevClip = gr.ClipBounds;
            gr.SetClip(mainWindow.ClientRectangle);

            int halfTickSizeInPx = (int)(Symbol.TickSize * mainWindow.YScaleFactor / 2.0);

            try
            {
                // Get left and right time from visible part or history
                DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
                DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);

                // Convert left and right time to index of bar
                int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
                int rightIndex = (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(rightTime));

                // Process only required (visible on the screen at the moment) range of bars
                for (int i = leftIndex; i <= rightIndex; i++)
                {
                    if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar
                        && bar.VolumeAnalysisData != null)
                    {
                        double val, vah;
                        this.CalculateValueArea(bar.VolumeAnalysisData.PriceLevels, out val, out vah);

                        int barLeftX = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar.TimeLeft));

                        int vahValueY = (int)mainWindow.CoordinatesConverter.GetChartY(vah) - halfTickSizeInPx;
                        gr.DrawLine(Pens.Green, barLeftX, vahValueY, barLeftX + this.CurrentChart.BarsWidth, vahValueY);


                        int valValueY = (int)mainWindow.CoordinatesConverter.GetChartY(val) + halfTickSizeInPx;
                        gr.DrawLine(Pens.Red, barLeftX, valValueY, barLeftX + this.CurrentChart.BarsWidth, valValueY);
                    }
                }
            }
            finally
            {
                gr.SetClip(prevClip);
            }
        }

        private void CalculateValueArea(Dictionary<double, VolumeAnalysisItem> priceLevels, out double val, out double vah)
        {
            // Convert priceLevels to sorted collection
            List<VolumeInfo> sortedPricesLevels = priceLevels.Select(item => new VolumeInfo() { Item = item.Value, Price = item.Key }).OrderBy(it => it.Price).ToList();

            // Find/cache required parameters: total value, max value, max value index
            double percent = this.ValueAreaPercent;
            VolumeAnalysisField field = VolumeAnalysisField.Volume;
            int MaxItemIndex = 0;
            double MaxItemValue = double.MinValue;
            int itemsCount = priceLevels.Count;
            double totalValue = 0;
            for (int i = 0; i < sortedPricesLevels.Count; i++)
            {
                double value = sortedPricesLevels[i].Item.GetValue(field);
                totalValue += value;

                if (value > MaxItemValue)
                {
                    MaxItemValue = value;
                    MaxItemIndex = i;
                }
            }

            // Calculate value area from MaxItemIndex item (POC)
            var upIndex = MaxItemIndex;
            var downIndex = MaxItemIndex;

            if (percent == 100)
            {
                upIndex = 0;
                downIndex = itemsCount - 1;
            }
            else
            {
                var valueArea = (percent / 100 * totalValue) - MaxItemValue;

                var prevUpIndex = upIndex;
                var prevDownIndex = downIndex;

                while (valueArea > 0)
                {
                    prevUpIndex = upIndex;
                    prevDownIndex = downIndex;

                    var upValue = 0d;
                    var downValue = 0d;

                    upIndex -= 1;
                    if (upIndex >= 0)
                        upValue += Math.Abs(sortedPricesLevels[upIndex].Item.GetValue(field));

                    downIndex += 1;
                    if (downIndex < itemsCount)
                        downValue += Math.Abs(sortedPricesLevels[downIndex].Item.GetValue(field));

                    if (upValue >= downValue && upValue != 0)
                    {
                        valueArea -= upValue;
                        downIndex = prevDownIndex;
                    }

                    if (upValue < downValue && downValue != 0)
                    {
                        valueArea -= downValue;
                        upIndex = prevUpIndex;
                    }

                    if (valueArea <= 0)
                    {
                        if (downIndex >= itemsCount)
                            downIndex = itemsCount - 1;

                        if (upIndex <= 0)
                            upIndex = 0;

                        break;
                    }

                    //
                    if (downIndex >= itemsCount && upIndex < 0)
                        break;
                }
            }

            val = sortedPricesLevels[upIndex].Price;
            vah = sortedPricesLevels[downIndex].Price;
        }

        protected override void OnInit() { }

        protected override void OnUpdate(UpdateArgs args) { }

        class VolumeInfo
        {
            public VolumeAnalysisItem Item;
            public double Price;
        }
    }
}
