// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API.REST.Models;
using System;

namespace OKExV5Vendor.API.REST.JsonConverters;

public class JsonCandleItemConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        try
        {
            var candle = new OKExCandleItem()
            {
                Time = DateTimeOffset.FromUnixTimeMilliseconds(jArray[0].Value<long>()).UtcDateTime,
                Open = jArray[1].Value<double>(),
                High = jArray[2].Value<double>(),
                Low = jArray[3].Value<double>(),
                Close = jArray[4].Value<double>(),
            };

            if (jArray.Count > 5)
                candle.Volume = jArray[5].Value<double>();

            if (jArray.Count > 6)
                candle.CurrencyVolume = jArray[6].Value<double>();

            if (jArray.Count > 7)
                candle.QuoteCurrencyVolume = jArray[7].Value<double>();

            return candle;
        }
        catch
        { }

        return null;
    }
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
}