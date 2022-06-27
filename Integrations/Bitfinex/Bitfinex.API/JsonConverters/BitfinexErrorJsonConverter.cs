// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexErrorJsonConverter : JsonConverter<BitfinexError>
    {
        public override BitfinexError ReadJson(JsonReader reader, Type objectType, BitfinexError existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                var jArray = serializer.Deserialize<JArray>(reader);

                var result = new BitfinexError
                {
                    Code = jArray[1].Value<int>(),
                    Text = jArray[2].Value<string>()
                };

                return result;
            }
            catch
            {
                string json = serializer?.Deserialize<JObject>(reader)?.ToString();
                return new BitfinexError
                {
                    Text = json
                };
            }
        }

        public override void WriteJson(JsonWriter writer, BitfinexError value, JsonSerializer serializer)
        { }
    }
}