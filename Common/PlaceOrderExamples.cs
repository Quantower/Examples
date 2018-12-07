using System.Linq;
using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class PlaceOrderExamples
    {
        public void Example()
        {
            // Get first available symbol
            Symbol symbol = Core.Instance.Symbols.FirstOrDefault();

            // Or you can find symbol by name
            symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "EUR/USD");

            // Get account by name
            Account account = Core.Instance.Accounts.FirstOrDefault(a => a.Name == "<account-name>");

            // Create request object
            var request = new PlaceOrderRequestParameters
            {
                Symbol = symbol,                        // Mandatory
                Account = account,                      // Mandatory
                Side = Side.Buy,                        // Mandatory
                OrderTypeId = OrderType.Market,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                Quantity = 0.5,                         // Mandatory

                Price = 1.52,                           // Optional. Required for limit and stop limit orders
                TriggerPrice = 1.52,                    // Optional. Required for stop and stop limit orders
                TrailOffset = 20,                       // Optional. Required for trailing stop order type
                
                TimeInForce = TimeInForce.Day,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
            };

            // Send request
            var result = Core.Instance.PlaceOrder(request);


            // Or you can use simplified method

            // Place market order with quantity = 1
            result = Core.Instance.PlaceOrder(symbol, account, Side.Buy);

            // Place limit order
            result = Core.Instance.PlaceOrder(symbol, account, Side.Sell, price: 1.5);

            // Place stop order
            result = Core.Instance.PlaceOrder(symbol, account, Side.Buy, triggerPrice: 2.1);

            // Place stop limit order
            result = Core.Instance.PlaceOrder(symbol, account, Side.Sell, timeInForce: TimeInForce.GTC, quantity: 0.6, price: 2.2, triggerPrice: 2.1);
        }
    }
}
