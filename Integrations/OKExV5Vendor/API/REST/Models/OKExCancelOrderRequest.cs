// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExCancelOrderRequest
    {
        [JsonProperty("ordId")]
        public string OrderId { get; private set; }

        [JsonProperty("instId")]
        public string InstrumentId { get; private set; }

        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        public OKExCancelOrderRequest(OKExSymbol symbol, string orderId)
        {
            this.InstrumentId = symbol.OKExInstrumentId;
            this.OrderId = orderId;
        }
    }
}
