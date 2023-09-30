// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Runtime.Serialization;
using Refit;

namespace Bitfinex.API.Models.Requests;

[DataContract]
public abstract class BitfinexRangeRequest
{
    [DataMember(Name = "start")]
    [AliasAs("start")]
    public long? Start { get; set; }

    [DataMember(Name = "end")]
    [AliasAs("end")]
    public long? End { get; set; }

    [DataMember(Name = "limit")]
    [AliasAs("limit")]
    public int? Limit { get; set; }

    // seems it's not working
    [DataMember(Name = "sort")]
    [AliasAs("sort")]
    public int? Sort { get; set; }
}