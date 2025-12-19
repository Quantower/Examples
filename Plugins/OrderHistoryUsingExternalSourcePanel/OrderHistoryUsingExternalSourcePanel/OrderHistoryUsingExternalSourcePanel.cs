
using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Plugins;
using TradingPlatform.PresentationLayer.Renderers.Table;


namespace OrderHistoryUsingExternalSourcePanel
{
    public class OrderHistoryUsingExternalSourcePanel : TablePlugin
    {
        #region Properties

        public static PluginInfo GetInfo() => new()
        {
            Name = "OrderHistoryUsingExternalSourcePanel",
            Title = loc.key("Ext. Orders history"),
            Group = PluginGroup.Portfolio,
            ShortName = "OHe",
            WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
            {
                BrowserUsageType = BrowserUsageType.None
            },
            CustomProperties = new Dictionary<string, object>()
            {
                {PluginInfo.Const.ALLOW_MANUAL_CREATION, true }
            }
        };

        protected override TableItem AssociatedTableItem => new OrderHistoryUsingExternalSourceTableItem();

        public override Size DefaultSize => new(this.UnitSize.Width * 3, this.UnitSize.Height);

        private string PathToCSV { get; set; } = @"C:\Users\Alex\Desktop\Orders history.csv";

        private string DataSeparator { get; set; } = ";";

        public override IList<SettingItem> Settings
        {
            get
            {
                var settings = base.Settings;

                settings.Add(new SettingItemFile("PathToCSV", this.PathToCSV, "csv files (.csv)|*.csv")
                {                    
                    Text = "Path to CSV file"
                });

                settings.Add(new SettingItemString("DataSeparator", this.DataSeparator) 
                {
                    Text = "Data separator"
                });

                return settings;
            }
            set
            {
                var holder = new SettingsHolder(value);

                base.Settings = value;

                if (holder.TryGetValue("PathToCSV", out var item))
                    this.PathToCSV = (string)item.Value;

                if (holder.TryGetValue("DataSeparator", out item))
                    this.DataSeparator = (string)item.Value;
            }
        }

        #endregion

        public override void Initialize()
        {
            base.Initialize();

            this.AlertsAllowed = true;
            this.AllowDataExport = true;
            this.CustomPluginTitle = new TablePluginTitle(this.table);

            this.ApplyFactorySettings();
        }

        public override void Populate(PluginParameters args = null)
        {
            if (string.IsNullOrEmpty(this.PathToCSV))
            {
                Core.Instance.Loggers.Log("OrderHistoryUsingExternalSourcePanel::Path to csv file was not specified");
                return;
            }

            if (!File.Exists(this.PathToCSV))
            {
                Core.Instance.Loggers.Log("OrderHistoryUsingExternalSourcePanel::csv file does not exists");
                return;
            }

            try
            {
                this.table.SaveGroupsState();
                this.table.SuspendDrawing = true;
                this.table.ClearAll();

                var lines = File.ReadAllLines(this.PathToCSV);

                foreach (var line in lines)
                    this.table.AddItem(new OrderHistoryUsingExternalSourceTableItem(line.Split(this.DataSeparator)));

                this.table.Regroup();
                this.table.SuspendDrawing = false;

                this.table.Resort();
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }
        }
    }
}
