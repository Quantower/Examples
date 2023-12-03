using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class LinkOrdersAsOCOExample
    {
        public void LinkOCO(List<IOrder> orders)
        {
            Task.Run(() =>
            {
                if (!orders.Any())
                    return;

                Core.Instance.SendCustomRequest(orders.First().ConnectionId, new LinkOCORequestParameters
                {
                    OrdersToLink = orders
                });
            });
        }
    }
}