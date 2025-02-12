
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace AccessOrderTypeAdvancedParameters
{
	public class AccessOrderTypeAdvancedParameters : Strategy
    {
        [InputParameter("Symbol")]
        Symbol symbol;

        [InputParameter("Account")]
        Account account;

        public AccessOrderTypeAdvancedParameters()
            : base()
        {            
            this.Name = "AccessOrderTypeAdvancedParameters";            
        }

        protected override void OnCreated() { }

        protected override void OnRun()
        {
            if (this.symbol == null || this.account == null)
            {
                this.Log($"Symbol or account is not selected");
                return;
            }

            OrderRequestParameters placeOrderRequest = new PlaceOrderRequestParameters() { Account = this.account, Symbol = this.symbol, Quantity = 1, Side = Side.Buy };
            FormatSettings formatSettings = new FormatSettings { DisplayQuantityInLots = true };

            foreach (var orderType in Core.Instance.OrderTypes.Where(o => o.Connection == this.symbol.Connection))
            {
                var orderTypeSettings = orderType.GetOrderSettings(placeOrderRequest, formatSettings);

                this.Log($"--------------------------------------------------");
                this.Log($"Order type: {orderType.Name} Advanced params count: {orderTypeSettings.Count}");
                this.Log($"--------------------------------------------------");

                foreach (var setting in orderTypeSettings)
                {
                    this.Log($"Setting name: '{setting.Name}' Type: '{setting.GetType().Name}'");

                    if (setting is SettingItemSelectorLocalized settItemSelector)
                        foreach (var item in settItemSelector.Items)
                            this.Log($"             Item name: '{item.Text}' Value: '{item.Value}'");
                }
            }
        }

        protected override void OnStop() { }    
        
        protected override void OnRemove() { }
    }
}
