// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System.Runtime.Serialization;

namespace Bitfinex.API.Models
{
    [DataContract]
    internal class BitfinexAuthMessage : BitfinexMessage
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }

        [DataMember(Name = "authSig")]
        public string Signature { get; set; }

        [DataMember(Name = "authPayload")]
        public string Payload { get; set; }

        [DataMember(Name = "authNonce")]
        public long Nonce { get; set; }

        public BitfinexAuthMessage()
        {
            this.Event = BitfinexEvent.AUTH;
        }
    }
}