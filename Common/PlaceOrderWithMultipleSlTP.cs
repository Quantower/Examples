using System.Linq;
using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class PlaceOrderWithMultipleSlTP : Strategy, ICurrentAccount, ICurrentSymbol
    {
        [InputParameter("Symbol", 0)]
        public Symbol CurrentSymbol { get; set; }

        /// <summary>
        /// Account to place orders
        /// </summary>
        [InputParameter("Account", 1)]
        public Account CurrentAccount { get; set; }

        public override string[] MonitoringConnectionsIds => new string[] { this.CurrentSymbol?.ConnectionId, this.CurrentAccount?.ConnectionId };

        public PlaceOrderWithMultipleSlTP()
            : base()
        {
            this.Name = "Place order with multiple SLTP";
        }

        protected override void OnRun()
        {
            base.OnRun();

            if (this.CurrentSymbol == null || this.CurrentAccount == null)
                return;

            if (Core.Instance.Positions.Any(p => p.Symbol == this.CurrentSymbol))
                return;


            PlaceOrderRequestParameters placeOrderParameters = new PlaceOrderRequestParameters()
            {
                Symbol = this.CurrentSymbol,
                Account = this.CurrentAccount,
                Quantity = 3,
                OrderTypeId = OrderType.Market,
                TimeInForce = TimeInForce.GTC
            };

            placeOrderParameters.StopLossItems.Add(SlTpHolder.CreateSL(10, PriceMeasurement.Offset, false, quantity: 2));
            placeOrderParameters.StopLossItems.Add(SlTpHolder.CreateSL(20, PriceMeasurement.Offset, false, quantity: 1));

            placeOrderParameters.TakeProfitItems.Add(SlTpHolder.CreateTP(15, PriceMeasurement.Offset, quantity: 2));
            placeOrderParameters.TakeProfitItems.Add(SlTpHolder.CreateTP(25, PriceMeasurement.Offset, quantity: 1));

            Core.Instance.PlaceOrder(placeOrderParameters);
        }
    }
}
