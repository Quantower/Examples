// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace Level2WithMBOIndicator
{
    public class Level2WithMBOIndicator : Indicator
    {
        private System.Threading.Timer timer;
        private bool needUpdateLevel2;
        private DepthOfMarketAggregatedCollections currentDom;
        private const int DOM_LEVELS_COUNT= 5;
        private const int DOM_UPDATE_FREQUENCY_MS = 500;

        public Level2WithMBOIndicator()
            : base()
        { 
            Name = "Level2WithMBOIndicator";            
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);
            SeparateWindow = false;
        }

        protected override void OnUpdate(UpdateArgs args)
        { }

        protected override void OnInit()
        {
            base.OnInit();

            this.Symbol.NewLevel2 += this.Symbol_NewLevel2;

            this.timer = new System.Threading.Timer(this.UpdateLevel2Data);
            this.timer.Change(TimeSpan.FromMilliseconds(DOM_UPDATE_FREQUENCY_MS), TimeSpan.FromMilliseconds(DOM_UPDATE_FREQUENCY_MS));

            this.needUpdateLevel2 = true;
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            Graphics gr = args.Graphics;

            Font f = new Font("Arial", 11, FontStyle.Regular, GraphicsUnit.Pixel);
            SolidBrush br = new SolidBrush(Color.AntiqueWhite);
            int startY = 50;
            int startX = 20;
            Dictionary<string, List<string>> sizesByPrices;

            if (this.currentDom != null)
            {
                //
                // Process asks
                //
                sizesByPrices = new Dictionary<string, List<string>>();

                for (int i = 0; i < this.currentDom.Asks.Count(); i++)
                {
                    string formattedPrice = this.Symbol.FormatPrice(this.currentDom.Asks[i].Price);

                    List<string> sizes;
                    if (!sizesByPrices.TryGetValue(formattedPrice, out sizes))
                        sizesByPrices[formattedPrice] = sizes = new List<string>();

                    if (sizesByPrices.Count <= DOM_LEVELS_COUNT)
                        sizes.Add(this.Symbol.FormatQuantity(this.currentDom.Asks[i].Size));
                    else
                        break;
                }

                //
                // Draw asks
                //
                foreach (var sizeItem in sizesByPrices.Keys.ToArray().OrderDescending())
                {
                    if (sizesByPrices[sizeItem].Count > 0)
                    {
                        gr.DrawString($"Ask: {sizeItem}    Sizes: {string.Join('|', sizesByPrices[sizeItem])}", f, br, startX, startY);
                        startY += 20;
                    }
                }

                //
                // Process bids
                //
                sizesByPrices = new Dictionary<string, List<string>>();

                for (int i = 0; i < this.currentDom.Bids.Count(); i++)
                {
                    string formattedPrice = this.Symbol.FormatPrice(this.currentDom.Bids[i].Price);

                    List<string> sizes;
                    if (!sizesByPrices.TryGetValue(formattedPrice, out sizes))
                        sizesByPrices[formattedPrice] = sizes = new List<string>();

                    if (sizesByPrices.Count <= DOM_LEVELS_COUNT)
                        sizes.Add(this.Symbol.FormatQuantity(this.currentDom.Bids[i].Size));
                    else
                        break;
                }

                //
                // Draw bids
                //
                foreach (var sizeItem in sizesByPrices.Keys.ToArray().OrderDescending())
                {
                    if (sizesByPrices[sizeItem].Count > 0)
                    {
                        gr.DrawString($"Bid: {sizeItem}    Sizes: {string.Join('|', sizesByPrices[sizeItem])}", f, br, startX, startY);
                        startY += 20;
                    }
                }
            }
            else
                gr.DrawString("DOM is not available yet", f, br, startX, startY);
            
        }

        private void UpdateLevel2Data(object obj)
        {
            if (this.needUpdateLevel2 && this.Symbol != null)
            {
                this.currentDom = this.Symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections(new GetDepthOfMarketParameters()
                {
                    GetLevel2ItemsParameters = new GetLevel2ItemsParameters()
                    {
                        AggregateMethod = AggregateMethod.None, // Need to disable aggregation to get original DOM levels
                        LevelsCount = 100,                        
                    }
                });

                this.needUpdateLevel2 = false;
            }
        }

        private void Symbol_NewLevel2(Symbol symbol, Level2Quote level2, DOMQuote dom)
            => this.needUpdateLevel2 = true;

        protected override void OnClear()
        {
            if (this.Symbol != null)
                this.Symbol.NewLevel2 -= this.Symbol_NewLevel2;

            if (this.timer != null)
                this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            base.OnClear();
        }
    }
}
