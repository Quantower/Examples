// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer.Options;

namespace GetOptionsInfo
{
    public class GetOptionsInfo : Indicator
    {
        int strikesCount = 2;
        IList<Symbol> callStrikes;
        IList<Symbol> putStrikes;
        Font font = new Font("Arial", 8, FontStyle.Regular);
        Brush textBrush = Brushes.LightBlue;
        BlackScholesPriceModel model = new BlackScholesPriceModel();

        public GetOptionsInfo()
            : base()
        {
            // Defines indicator's name and description.
            Name = "GetOptionsInfo";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            // Loading options symbols may take some time, so it is better to do in a separate thread
            Task.Run(() =>
            {
                // Load options series for current symbol
                var series = Core.Instance.GetOptionSeries(this.Symbol);
                if (series != null && series.Count > 0)
                {
                    // Load strikes for first nearest serie
                    var strikes = Core.Instance.GetStrikes(series.OrderBy(s => s.ExpirationDate).First());
                    if (strikes != null)
                    {
                        // Get call options nearest to current last price
                        callStrikes = new List<Symbol>();
                        callStrikes.AddRange(strikes.Where(s => s.StrikePrice <= this.Symbol.Last && s.OptionType == OptionType.Call).OrderByDescending(s => s.StrikePrice).Take(strikesCount).OrderBy(s => s.StrikePrice));
                        callStrikes.AddRange(strikes.Where(s => s.StrikePrice >= this.Symbol.Last && s.OptionType == OptionType.Call).OrderBy(s => s.StrikePrice).Take(strikesCount));

                        // Get put options nearest to current last price
                        putStrikes = new List<Symbol>();
                        putStrikes.AddRange(strikes.Where(s => s.StrikePrice <= this.Symbol.Last && s.OptionType == OptionType.Put).OrderByDescending(s => s.StrikePrice).Take(strikesCount).OrderBy(s => s.StrikePrice));
                        putStrikes.AddRange(strikes.Where(s => s.StrikePrice >= this.Symbol.Last && s.OptionType == OptionType.Put).OrderBy(s => s.StrikePrice).Take(strikesCount));

                        // Subscribe for Last and Bid/Ask quotes
                        foreach (var strike in callStrikes.Concat(putStrikes))
                        {
                            strike.NewLast += Strike_NewMessage;
                            strike.NewQuote += Strike_NewMessage;
                        }
                    }
                }
            });
        }

        private void Strike_NewMessage(Symbol symbol, MessageQuote quote) { }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (callStrikes == null || putStrikes == null)
                return;

            // Draw call options
            args.Graphics.DrawString($"CALL {callStrikes[0].ExpirationDate.ToShortDateString()}", font, textBrush, 30, 50);
            int textY = 70;
            foreach (var strike in callStrikes)
            {
                double iv = model.IV(strike, OptionPriceType.Ask, 0, 0);
                args.Graphics.DrawString($"Strike: {strike.FormatPrice(strike.StrikePrice)} Ask: {strike.FormatPrice(strike.Ask)} IV: {Math.Round(iv * 100, 2)}% Gamma: {model.Gamma(strike, iv, 0, 0).ToString("N5")}", font, textBrush, 30, textY);
                textY += 20;
            }

            // Draw put options
            args.Graphics.DrawString($"PUT {putStrikes[0].ExpirationDate.ToShortDateString()}", font, textBrush, 400, 50);
            textY = 70;
            foreach (var strike in putStrikes)
            {
                double iv = model.IV(strike, OptionPriceType.Ask, 0, 0);
                args.Graphics.DrawString($"Strike: {strike.FormatPrice(strike.StrikePrice)} Ask: {strike.FormatPrice(strike.Ask)} IV: {Math.Round(iv * 100, 2)}% Gamma: {model.Gamma(strike, iv, 0, 0).ToString("N5")}", font, textBrush, 400, textY);
                textY += 20;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            // Unsubscribe from quotes for all options symbols
            foreach (var strike in callStrikes.Concat(putStrikes))
            {
                strike.NewLast -= Strike_NewMessage;
                strike.NewQuote -= Strike_NewMessage;
            }
        }
    }
}
