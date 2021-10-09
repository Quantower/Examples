using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Plugins;

namespace GDIBasedPlugin
{
    public class GDIBasedPlugin : Plugin
    {
        private GDIRenderer gdiRenderer;

        /// <summary>
        /// Plugin meta information
        /// </summary>
        public static PluginInfo GetInfo()
        {
            var windowParameters = NativeWindowParameters.Panel;
            windowParameters.AllowDrop = true;
            windowParameters.BrowserUsageType = BrowserUsageType.None;

            return new PluginInfo()
            {
                Name = "GDIBasedPlugin",
                Title = "GDIBasedPlugin",
                Group = PluginGroup.Misc,
                ShortName = "GDI",
                SortIndex = 35,
                AllowSettings = true,
                WindowParameters = windowParameters,
                CustomProperties = new Dictionary<string, object>()
                {
                    {PluginInfo.Const.ALLOW_MANUAL_CREATION, true }
                }
            };
        }

        /// <summary>
        /// Default plugin size - can be an absolute value or a multiple of UnitSize (UnitSize depends on the monitor resolution)
        /// </summary>
        public override Size DefaultSize => new Size(this.UnitSize.Width * 1, this.UnitSize.Height * 2);

        /// <summary>
        /// Initialize called once on plugin creation
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            //
            this.gdiRenderer = new GDIRenderer(this.Window.CreateRenderingControl("GDIRenderer"));
        }

        /// <summary>
        /// Populate called on plugin creation and each time when any connection get connected/disconnected
        /// </summary>
        public override void Populate(PluginParameters args = null)
        {
            base.Populate(args);

            // Redraw renderer
            this.gdiRenderer.RedrawBufferedGraphic();
        }

        public override void Dispose()
        {
            // 
            if (this.gdiRenderer != null)
            {
                this.gdiRenderer.Dispose();
                this.gdiRenderer = null;
            }

            base.Dispose();
        }

        public override IList<SettingItem> Settings 
        {
            get
            {
                var result = base.Settings;

                // Here you can specify customf settings for your plugin
                result.Add(new SettingItemColor("Color", this.gdiRenderer.Color));

                return result;
            }
            set
            {
                base.Settings = value;

                // Apply custom settings
                if (value.GetItemByPath("Color") is SettingItemColor color)
                {
                    this.gdiRenderer.Color = (Color)color.Value;
                    this.gdiRenderer.RedrawBufferedGraphic();
                }
            }
        }

        protected override void OnLayoutUpdated()
        {
            base.OnLayoutUpdated();

            if (this.gdiRenderer != null)
                this.gdiRenderer.Layout.Margin = this.NonClientMargin;
        }
    }
}
