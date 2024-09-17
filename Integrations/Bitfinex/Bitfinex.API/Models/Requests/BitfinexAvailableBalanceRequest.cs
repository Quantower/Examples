// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Runtime.Serialization;

namespace Bitfinex.API.Models.Requests;

[DataContract]
public class BitfinexAvailableBalanceRequest
{
    [DataMember(Name = "symbol")]
    public string Symbol { get; set; }

    [DataMember(Name = "dir")]
    public int Direction { get; set; }

    [DataMember(Name = "rate")]
    public string Rate { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "lev")]
    public int? Leverage { get; set; }
}