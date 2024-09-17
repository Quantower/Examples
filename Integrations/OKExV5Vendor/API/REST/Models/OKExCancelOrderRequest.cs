// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExCancelOrderRequest
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
