// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class SettingsRelationsExample : Strategy // could be an Indicator also
    {
        private int someSelectorField = 1;
        private double firstDependentField = 0.01;
        private DateTime secondDependentField;
        private string thirdDependentField;

        public override IList<SettingItem> Settings
        {
            get
            {
                var settings = base.Settings;

                var firstOption = new SelectItem("First option", 1);
                var secondOption = new SelectItem("Second option", 2);

                settings.Add(new SettingItemSelectorLocalized("someSelectorField", new SelectItem("", this.someSelectorField), new List<SelectItem>
                             {
                                 firstOption,
                                 secondOption
                             })
                             {
                                 Text = "Selector",
                                 SortIndex = 10
                             });

                settings.Add(new SettingItemDouble("firstDependentField", this.firstDependentField)
                {
                    Text = "First field",
                    SortIndex = 20,
                    Relation = new SettingItemRelationVisibility("someSelectorField", firstOption)  // will be visible only when first option will be selected
                });

                settings.Add(new SettingItemDateTime("secondDependentField", this.secondDependentField)
                {
                    Text = "Second field",
                    SortIndex = 20,
                    Relation = new SettingItemRelationVisibility("someSelectorField", secondOption)  // will be visible only when second option will be selected
                });

                settings.Add(new SettingItemString("thirdDependentField", this.thirdDependentField)
                {
                    Text = "Third field",
                    SortIndex = 30,
                    Relation = new SettingItemRelationEnability("someSelectorField", secondOption)  // will be enabled only when second option will be selected
                });

                return settings;
            }
            set
            {
                if (value.TryGetValue("someSelectorField", out int selectorValue))
                    this.someSelectorField = selectorValue;

                if (value.TryGetValue("firstDependentField", out double firstFieldValue))
                    this.firstDependentField = firstFieldValue;

                if (value.TryGetValue("secondDependentField", out DateTime secondFieldValue))
                    this.secondDependentField = secondFieldValue;

                if (value.TryGetValue("thirdDependentField", out string thirdFieldValue))
                    this.thirdDependentField = thirdFieldValue;
            }
        }

        public SettingsRelationsExample()
            : base()
        { }
    }
}