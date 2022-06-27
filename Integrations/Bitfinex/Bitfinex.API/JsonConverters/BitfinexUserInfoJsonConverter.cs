// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexUserInfoJsonConverter : JsonConverter<BitfinexUserInfo>
    {
        public override BitfinexUserInfo ReadJson(JsonReader reader, Type objectType, BitfinexUserInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexUserInfo()
            {
                Id = jArray[0].Value<int>(),
                Email = jArray[1].Value<string>(),
                Username = jArray[2].Value<string>(),
                CreationTime = DateTimeOffset.FromUnixTimeMilliseconds(jArray[3].Value<long>()).UtcDateTime,
                IsVerified = jArray[4].Value<int>() == 1,
                VerificationLevel = jArray[5].Value<int>(),
                Timezone = jArray[7].Value<string>(),
                Locale = jArray[8].Value<string>(),
                Company = jArray[9].Value<string>(),
                IsMerchantEnabled = jArray[10].Value<int>() == 1
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexUserInfo value, JsonSerializer serializer)
        { }
    }
}