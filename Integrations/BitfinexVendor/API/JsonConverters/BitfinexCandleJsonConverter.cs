using BitfinexVendor.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BitfinexVendor.API.JsonConverters
{
    class BitfinexCandleJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexCandle()
            {
                Timestamp = jArray[0].Value<long>(),
                Open = jArray[1].Value<decimal>(),
                Close = jArray[2].Value<decimal>(),
                High = jArray[3].Value<decimal>(),
                Low = jArray[4].Value<decimal>(),
                Volume = jArray[5].Value<decimal>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        { }
    }
}
