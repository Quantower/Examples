// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace TestIndicator;

public class FitDrawingTestIndicator : Indicator
{
    [InputParameter]
    public int Multiplier { get; set; }

    private readonly List<double> values;

    public FitDrawingTestIndicator()
    {
        this.Name = nameof(FitDrawingTestIndicator);

        this.Multiplier = 1;
        this.values = new List<double>();
    }

    protected override void OnUpdate(UpdateArgs args)
    {
        while(this.values.Count < this.Count)
            this.values.Add(Const.DOUBLE_UNDEFINED);

        this.values[this.Count - 1] = this.Close() * this.Multiplier;
    }

    protected override bool OnTryGetMinMax(int fromOffset, int toOffset, out double min, out double max)
    {
        min = Const.DOUBLE_UNDEFINED;
        max = Const.DOUBLE_UNDEFINED;

        for (int i = fromOffset; i <= toOffset; i++)
        {
            int index = this.Count - 1 - i;

            double value = this.values[index];

            if (double.IsNaN(value))
                continue;

            min = double.IsNaN(min) ? value : Math.Min(min, value);
            max = double.IsNaN(max) ? value : Math.Max(max, value);
        }

        return !double.IsNaN(min) && !double.IsNaN(max);
    }

    public override void OnPaintChart(PaintChartEventArgs args)
    {
        var gr = args.Graphics;

        var points = new Point[this.values.Count];
        var converter = this.CurrentChart.Windows[args.WindowIndex].CoordinatesConverter;

        for (int i = 0; i < this.values.Count; i++)
        {
            double price = this.values[i];
            var time = this.Time(this.values.Count - 1 - i);

            int x = (int)converter.GetChartX(time);
            int y = (int)converter.GetChartY(price);

            var point = new Point(x, y);

            if (!args.Rectangle.Contains(point))
                continue;

            points[i] = point;
        }

        gr.DrawLines(Pens.Red, points);
    }
}