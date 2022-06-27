// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexNotificationJsonConverter : JsonConverter<BitfinexNotification>
    {
        public override BitfinexNotification ReadJson(JsonReader reader, Type objectType, BitfinexNotification existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexNotification
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(jArray[0].Value<long>()).UtcDateTime,
                Type = jArray[1].Value<string>(),
                Status = jArray[6].Value<string>(),
                Text = jArray[7].Value<string>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexNotification value, JsonSerializer serializer)
        { }
    }
}