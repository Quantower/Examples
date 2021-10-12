// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Modules;

namespace RepeatOrderPlacing
{
	/// <summary>
    /// An example of order placing strategy that repeat placing order few times according to specified parameters: RepeatCount and DelaySeconds
    /// </summary>
    public class RepeatOrderPlacing : OrderPlacingStrategy
    {
        [InputParameter]
        public int DelaySeconds = 10;

        [InputParameter]
        public int RepeatCount = 3;

        bool cancelled = false;

        public RepeatOrderPlacing()
            : base()
        {
            this.Name = "RepeatOrderPlacing";
        }

        protected override void OnPlaceOrder(PlaceOrderRequestParameters placeOrderRequest)
        {
            // For trading logs
            placeOrderRequest.SendingSource = this.Name;

            for (int i = 0; i < this.RepeatCount; i++)
            {
                // Place order
                this.Log($"Place order #{i + 1}");

                var result = Core.Instance.PlaceOrder(placeOrderRequest);
                if (result.Status == TradingOperationResultStatus.Failure)
                    throw new Exception(result.Message);

                // Wait before placing next order
                this.Log($"Wait {this.DelaySeconds} seconds...");
                
                Task.Delay(TimeSpan.FromSeconds(this.DelaySeconds)).Wait();

                // Strategy executing was cancelled
                if (this.cancelled)
                    return;
            }
        }

        protected override void OnCancel()
        {
            this.cancelled = true;
        }
    }
}
