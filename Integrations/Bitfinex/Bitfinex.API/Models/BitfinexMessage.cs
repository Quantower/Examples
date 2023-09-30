// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Runtime.Serialization;

namespace Bitfinex.API.Models;

[DataContract]
internal class BitfinexMessage
{
    [DataMember(Name = "event")]
    public string Event { get; set; }

    [DataMember(Name = "chanId")]
    public string ChannelId { get; set; }

    [DataMember(Name = "channel")]
    public string ChannelName { get; set; }

    [DataMember(Name = "msg")]
    public string Message { get; set; }

    [DataMember(Name = "code")]
    public int? ErrorCode { get; set; }

    [DataMember(Name = "pair")]
    public string Pair { get; set; }

    [DataMember(Name = "prec")]
    public string Precision { get; set; }

    [DataMember(Name = "len")]
    public int? Length { get; set; }

    [DataMember(Name = "userId")]
    public int? UserId { get; set; }

    public BitfinexMessage()
    { }

    public BitfinexMessage(string channelName)
    {
        this.ChannelName = channelName;
    }

    public string FormatError() => $"{this.ErrorCode}: {this.Message}";
}