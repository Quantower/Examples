// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Runtime.Serialization;

namespace Bitfinex.API.Models;

[DataContract]
public class BitfinexMeta
{
    [DataMember(Name = "aff_code")]
    public string AffiliateCode { get; set; }

    [DataMember(Name = "$F7")]
    public int? IsPostOnly { get; set; }

    [DataMember(Name = "$F33")]
    public int? Leverage { get; set; }
}