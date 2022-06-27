// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexWalletJsonConverter : JsonConverter<BitfinexWallet>
    {
        public override BitfinexWallet ReadJson(JsonReader reader, Type objectType, BitfinexWallet existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexWallet
            {
                Type = jArray[0].Value<string>(),
                Currency = jArray[1].Value<string>(),
                Balance = jArray[2].Value<decimal>(),
                UnsettledInterest = jArray[3].Value<decimal?>(),
                AvailableBalance = jArray[4].Value<decimal?>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexWallet value, JsonSerializer serializer)
        { }
    }
}