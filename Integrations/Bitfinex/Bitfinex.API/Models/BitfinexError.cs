// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Bitfinex.API.JsonConverters;
using Newtonsoft.Json;

namespace Bitfinex.API.Models
{

    [JsonConverter(typeof(BitfinexErrorJsonConverter))]
    public class BitfinexError
    {
        public int Code { get; internal set; }

        public string Text { get; internal set; }
    }
}