// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace MovingAverageIndicators;

public class DoubleSMAIndicator : Indicator
{
    #region Parameters

    private const PriceType DOUBLE_SMA_SOURCE_TYPE = PriceType.Close;

    private Indicator sma;
    private Indicator doubleSma;
    private HistoricalDataCustom smaSourceHD;

    [InputParameter("Period of SMA", 10)]
    public int Period = 20;

    [InputParameter("Period of double SMA", 20)]
    public int DoublePeriod = 20;

    #endregion Paramaeters

    public DoubleSMAIndicator()
    {
        this.Name = "Double SMA indicator";

        this.AddLineSeries("SMA", Color.Orange, 1, LineStyle.Solid);
        this.AddLineSeries("Double SMA", Color.CadetBlue, 1, LineStyle.Solid);

        this.SeparateWindow = true;
    }

    #region Base overrides

    protected override void OnInit()
    {
        // Create first 'SMA' indicator and attach it to main HistoricalData
        this.sma = Core.Instance.Indicators.BuiltIn.SMA(this.Period, PriceType.Close);
        this.AddIndicator(this.sma);

        // Create custom HistoricalData instance. In 'OnUpdate' we will fill it values from SMA indicator above.
        this.smaSourceHD = new HistoricalDataCustom(this);

        // Create second 'SMA' indicator and attach it to 'HistoricalDataCustom' (smaSourceHD)
        // HistoricalDataCustom is a kind of buffer of custom values. We can fill it with our own values and attach various indicators to it.
        // All of these indicators will use "custom" values as their source.
        this.doubleSma = Core.Instance.Indicators.BuiltIn.SMA(this.DoublePeriod, DOUBLE_SMA_SOURCE_TYPE);
        this.smaSourceHD.AddIndicator(this.doubleSma);
    }
    protected override void OnUpdate(UpdateArgs args)
    {
        if (this.Period >= this.Count)
            return;

        var smaValue = this.sma.GetValue();

        // set 'smaValue' to custom historical data.
        // the 'doubleSma' indicator will use this value as 'Close' source.
        this.smaSourceHD[DOUBLE_SMA_SOURCE_TYPE] = smaValue;

        var doubleSmaValue = this.doubleSma.GetValue();

        this.SetValue(smaValue, 0);
        this.SetValue(doubleSmaValue, 1);
    }
    protected override void OnClear()
    {
        this.sma?.Dispose();
        this.doubleSma?.Dispose();
        this.smaSourceHD?.Dispose();
    }

    #endregion Base overrides
}