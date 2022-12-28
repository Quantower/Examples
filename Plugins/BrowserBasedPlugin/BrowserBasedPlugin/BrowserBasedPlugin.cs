using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Plugins;

namespace BrowserBasedPlugin
{
    public class BrowserBasedPlugin : Plugin
    {
        public static PluginInfo GetInfo()
        {
            return new PluginInfo()
            {
                Name = "BrowserBasedPlugin",
                Title = loc.key("Browser Based Plugin"),
                Group = PluginGroup.Trading,
                ShortName = "BBP",
                TemplateName = "layout.html",
                FolderName = "../../../../Settings/Scripts/plug-ins/BrowserBasedPlugin",
                WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
                {
                    AllowsTransparency = false,
                    ResizeMode = NativeResizeMode.CanResize,
                    HeaderVisible = true,
                    BindingBehaviour = BindingBehaviour.Bindable,
                    StickingEnabled = StickyWindowBehavior.AllowSticking,
                },
                CustomProperties = new Dictionary<string, object>()
                {                
                    {PluginInfo.Const.ALLOW_MANUAL_CREATION, true }
                }
            };
        }
        public override Size DefaultSize => new Size(800, 600);

        public override void Initialize()
        {
            base.Initialize();

            // Subscribe to JS event
            this.Window.Browser.AddEventHandler("loginbutton", "onclick", OnLoginButtonClick);
            this.Window.Browser.AddEventHandler("clearbutton", "onclick", OnClearButtonClick);
            this.Window.Browser.AddEventHandler("callJSFunctionbutton", "onclick", OnCallJSFunctionButtonClick);
        }

        private void OnLoginButtonClick(string elementId, object args)
        {
            // Get data from HTML elements by element id
            string login = this.Window.Browser.GetHtmlValue("usernametextbox", HtmlGetValueAction.GetProperty, "value").Result.ToString();
            string password = this.Window.Browser.GetHtmlValue("passwordtextbox", HtmlGetValueAction.GetProperty, "value").Result.ToString();

            Application.Instance.Notifications.ShowConfirmation("Entered Login: " + login + " Password: " + password, null);
        }

        private void OnClearButtonClick(string elementId, object args)
        {
            // Set value for HTML element by element id
            this.Window.Browser.UpdateHtml("usernametextbox", HtmlAction.SetValueString, string.Empty);
            this.Window.Browser.UpdateHtml("passwordtextbox", HtmlAction.SetValueString, string.Empty);
        }

        private void OnCallJSFunctionButtonClick(string elementId, object args)
        {
            // Execute JS function with parameter
            this.Window.Browser.UpdateHtml(string.Empty, HtmlAction.InvokeJs, "JSFunction('Text from JS')");
        }
    }
}
