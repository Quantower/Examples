// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Runtime.Serialization;

namespace Bitfinex.API.Models.Requests;

[DataContract]
public class BitfinexSubmitOrderRequest : BitfinexOrderRequest
{
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "symbol")]
    public string Symbol { get; set; }

    [DataMember(Name = "price_oco_stop")]
    public string PriceOcoStop { get; set; }
}