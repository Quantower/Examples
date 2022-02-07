// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using System.Linq;
using System.Collections.Generic;

namespace AccessIndicatorByName
{
	public class AccessIndicatorByName : Indicator
    {
        Indicator emaInd;

        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public AccessIndicatorByName()
            : base()
        {
            // Defines indicator's name and description.
            Name = "AccessIndicatorByName";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            // Create instance of required indicator
            IndicatorInfo emaIndicatorInfo = Core.Instance.Indicators.All.FirstOrDefault(info => info.Name == "Exponential Moving Average");
            emaInd = Core.Instance.Indicators.CreateIndicator(emaIndicatorInfo);

            // Set indicator parameters
            emaInd.Settings = new List<SettingItem>
            {
                new SettingItemInteger("MaPeriod", 10)
            };

            // Add
            AddIndicator(emaInd);
        }
   
        protected override void OnUpdate(UpdateArgs args)
        {
            // Get value from inner indicator
            SetValue(emaInd.GetValue(0, 0));
        }
    }
}
