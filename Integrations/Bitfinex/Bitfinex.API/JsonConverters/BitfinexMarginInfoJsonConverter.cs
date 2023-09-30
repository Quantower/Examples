// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters;

internal class BitfinexMarginInfoJsonConverter : JsonConverter<BitfinexMarginInfo>
{
    public override BitfinexMarginInfo ReadJson(JsonReader reader, Type objectType, BitfinexMarginInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        string type = jArray[0].Value<string>();

        BitfinexMarginInfo result;

        if (type == BitfinexMarginKey.BASE)
        {
            var data = jArray[1].Value<JArray>();

            result = new BitfinexMarginInfo
            {
                UserPnL = data[0].Value<decimal>(),
                UserSwaps = data[1].Value<decimal>(),
                MarginBalance = data[2].Value<decimal>(),
                MarginNet = data[3].Value<decimal>(),
                MarginMin = data[4].Value<decimal>()
            };
        }
        else
        {
            var data = jArray[2].Value<JArray>();

            result = new BitfinexMarginInfo
            {
                Symbol = jArray[1].Value<string>(),
                TradableBalance = data[0].Value<decimal>(),
                GrossBalance = data[1].Value<decimal>(),
                Buy = data[2].Value<decimal>(),
                Sell = data[3].Value<decimal>()
            };
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, BitfinexMarginInfo value, JsonSerializer serializer) { }
}