// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Runtime.Serialization;

namespace Bitfinex.API.Models.Requests;

[DataContract]
public class BitfinexOrderRequest
{
    [DataMember(Name = "gid")]
    public long? GroupId { get; set; }

    [DataMember(Name = "cid")]
    public long? ClientOrderId { get; set; }

    [DataMember(Name = "price")]
    public string Price { get; set; }

    [DataMember(Name = "amount")]
    public string Amount { get; set; }

    [DataMember(Name = "flags")]
    public BitfinexOrderFlags Flags { get; set; }

    [DataMember(Name = "price_trailing")]
    public string TrailingPrice { get; set; }

    [DataMember(Name = "price_aux_limit")]
    public string AuxiliaryLimitPrice { get; set; }

    [DataMember(Name = "tif")]
    public DateTime? TimeInForce { get; set; }

    [DataMember(Name = "lev")]
    public int? Leverage { get; set; }

    [DataMember(Name = "meta")]
    public BitfinexMeta Meta { get; set; }
}