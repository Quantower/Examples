// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;

namespace OKExV5Vendor.API.REST.Models;

internal class OKExAmendOrderRequest
{
    [JsonProperty("instId")]
    public string InstrumentId { get; set; }

    [JsonProperty("ordId")]
    public string OrderId { get; set; }

    [JsonProperty("newSz")]
    public string NewSize { get; set; }

    [JsonProperty("newPx")]
    public string NewPrice { get; set; }

    [JsonProperty("clOrdId")]
    public string ClientOrderId { get; set; }

    public OKExAmendOrderRequest(OKExSymbol symbol, string orderId)
    {
        this.InstrumentId = symbol.OKExInstrumentId;
        this.OrderId = orderId;
    }
}