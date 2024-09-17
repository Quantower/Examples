// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;
using System.Linq;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters;

internal class BitfinexOrderResponseJsonConverter : JsonConverter<BitfinexOrderResponse>
{
    public override BitfinexOrderResponse ReadJson(JsonReader reader, Type objectType, BitfinexOrderResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        var ordersItem = jArray[4].Value<JArray>();
        var orders = ordersItem[0].Type == JTokenType.Array ?
            ordersItem!.Select(a => serializer.Deserialize<BitfinexOrder>(a.CreateReader())).ToArray() :
            new[] { serializer.Deserialize<BitfinexOrder>(ordersItem.CreateReader()) };

        var result = new BitfinexOrderResponse
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(jArray[0].Value<long>()).UtcDateTime,
            Type = jArray[1].Value<string>(),
            Orders = orders,
            Status = jArray[6].Value<string>(),
            Text = jArray[7].Value<string>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, BitfinexOrderResponse value, JsonSerializer serializer) { }
}