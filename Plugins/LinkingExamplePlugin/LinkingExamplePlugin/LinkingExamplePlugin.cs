using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Plugins;
using TradingPlatform.PresentationLayer.Plugins.Services.Linking;
using TradingPlatform.PresentationLayer.Plugins.Services.Linking.EventArgs;
using TradingPlatform.PresentationLayer.Plugins.Services.Linking.Models.Scopes;
using TradingPlatform.PresentationLayer.Plugins.ViewControllers;

namespace LinkingExamplePlugin
{
    public class LinkingExamplePlugin : Plugin, ILinkable
    {
        public override Symbol CurrentSymbol
        {
            get => this.currentSymbol;
            set
            {
                this.currentSymbol = value;

                this.Window.Browser.UpdateHtml("symboltextbox", HtmlAction.SetValueString, value == null ? string.Empty : value.ToString());
            }
        }
        private Symbol currentSymbol;

        public static PluginInfo GetInfo()
        {
            return new PluginInfo()
            {
                Name = "LinkingExamplePlugin",
                Title = loc.key("Linking example plugin"),
                Group = PluginGroup.Trading,
                ShortName = "LEP",
                TemplateName = "layout.html",
                WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
                {
                    AllowsTransparency = true,
                    ResizeMode = NativeResizeMode.NoResize,
                    HeaderVisible = true,
                    BindingBehaviour = BindingBehaviour.Bindable,
                    StickingEnabled = StickyWindowBehavior.AllowSticking,
                    AllowMaximizeButton = false,
                    AllowFullScreenButton = false,
                    AllowActionsButton = true,
                    AllowCloseButton = true
                },
                CustomProperties = new Dictionary<string, object>()
                {
                    {PluginInfo.Const.ALLOW_MANUAL_CREATION, true }
                }
            };
        }
        public override Size DefaultSize => new Size(440, 400);

        public override void Initialize()
        {
            base.Initialize();

            // Subscribe to JS event
            this.Window.Browser.AddEventHandler("symbollookupbutton", "onclick", OnSymbolLookupButtonClick);

            this.RegisterService<LinkingPluginService>();
        }

        private void OnSymbolLookupButtonClick(string elementId, object args)
        {
            var parameters = new PluginParameters()
             .Add(PluginParameters.CALLBACK, new Action<IList<Symbol>>(this.SymbolSelectionCallback))
             .Add("SelectionMode", SelectionMode.SingleSelect)
             .Add(PluginParameters.WINDOW_POSITION_TYPE, NativeWindowDefaultPositionType.CenterScreenByCurrentMousePosition)
             .Add(PluginParameters.SYMBOL, this.CurrentSymbol);

            Application.Instance.SendCommand(new ApplicationCommandOpenPlugin { Name = Plugin.SYMBOLS_LOOKUP, Parameters = parameters });
        }

        private void SymbolSelectionCallback(IList<Symbol> symbols)
        {
            this.CurrentSymbol = symbols?.FirstOrDefault();

            this.PublishLinking(LinkingScope.Symbol, this.CurrentSymbol);
        }

        public LinkingState LinkingState => this.GetService<LinkingPluginService>().LinkingState;

        public event EventHandler<LinkingEntityEventArgs> LinkingStateChanged
        {
            add { this.GetService<LinkingPluginService>().LinkingStateChanged += value; }
            remove { this.GetService<LinkingPluginService>().LinkingStateChanged -= value; }
        }
    }
}
