// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace UsingInnerIndicatorSettings
{
    public class UsingInnerIndicatorSettings : Indicator
    {
        Indicator superTrendInd;

        IList<SettingItem> superTrendIndSettings;

        public UsingInnerIndicatorSettings()
            : base()
        {
            // Defines indicator's name and description.
            Name = "UsingInnerIndicatorSettings";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            // Create instance of required indicator
            IndicatorInfo emaIndicatorInfo = Core.Instance.Indicators.All.FirstOrDefault(info => info.Name == "SuperTrend");
            superTrendInd = Core.Instance.Indicators.CreateIndicator(emaIndicatorInfo);

            // Use previous stored settings
            if (superTrendIndSettings != null)
            {
                superTrendInd.Settings = superTrendIndSettings;
            }
            // Set default settings
            else
            {
                superTrendInd.Settings = new List<SettingItem>
                {
                    new SettingItemInteger("ATR period", 11),
                    new SettingItemDouble("Digit", 4),
                };
            }

            // Add indicator
            AddIndicator(superTrendInd);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            // Get value from inner indicator
            SetValue(superTrendInd.GetValue(0, 0), 0);
        }

        public override IList<SettingItem> Settings
        {
            get
            {
                var result = base.Settings;

                if (superTrendInd != null)
                {
                    // Get all visible input parameters from Super Trend indicator
                    var superTrendIndSettings = superTrendInd.Settings.Where(s => s.SeparatorGroup != null && s.SeparatorGroup.Text == "Input parameters" && s.VisibilityMode == VisibilityMode.Visible).ToList();

                    // Put Super Trend indicator settings into separate group on Settings Screen
                    SettingItemSeparatorGroup superTrendIndSeparator = new SettingItemSeparatorGroup("Super Trend");
                    foreach (var s in superTrendIndSettings)
                        s.SeparatorGroup = superTrendIndSeparator;

                    // Add Super Trend indicator settings in special SettingItemGroup
                    result.Add(new SettingItemGroup("SuperTrend", superTrendIndSettings));
                }
                return result;
            }
            set
            {
                base.Settings = value;

                // Apply Super Trend indicator settings
                if (value.GetItemByName("SuperTrend") is SettingItemGroup superTrendIndSettings && superTrendInd != null)
                {
                    superTrendInd.Settings = (List<SettingItem>)superTrendIndSettings.Value;

                    Refresh();
                }
            }
        }

        protected override void OnClear()
        {
            base.OnClear();

            if (superTrendInd != null)
            {
                // Remember current settings
                superTrendIndSettings = superTrendInd.Settings;

                // Remove indicator
                RemoveIndicator(this.superTrendInd);
                superTrendInd.Dispose();
                superTrendInd = null;
            }
        }
    }
}
