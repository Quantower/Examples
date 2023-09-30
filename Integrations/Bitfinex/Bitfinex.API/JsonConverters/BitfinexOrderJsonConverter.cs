// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters;

internal class BitfinexOrderJsonConverter : JsonConverter<BitfinexOrder>
{
    public override BitfinexOrder ReadJson(JsonReader reader, Type objectType, BitfinexOrder existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        long? expireTime = jArray[10].Value<long?>();

        var result = new BitfinexOrder
        {
            Id = jArray[0].Value<long>(),
            GroupId = jArray[1].Value<long?>(),
            ClientOrderId = jArray[2].Value<long>(),
            Symbol = jArray[3].Value<string>(),
            CreationTime = DateTimeOffset.FromUnixTimeMilliseconds(jArray[4].Value<long>()).UtcDateTime,
            UpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(jArray[5].Value<long>()).UtcDateTime,
            Amount = jArray[6].Value<double>(),
            OriginalAmount = jArray[7].Value<double>(),
            Type = jArray[8].Value<string>(),
            PreviousType = jArray[9].Value<string>(),
            ExpirationTime = expireTime != null ? DateTimeOffset.FromUnixTimeMilliseconds(expireTime.Value).UtcDateTime : null,
            Flags = (BitfinexOrderFlags)jArray[12].Value<int>(),
            Status = jArray[13].Value<string>(),
            Price = jArray[16].Value<double>(),
            AveragePrice = jArray[17].Value<double>(),
            TrailingPrice = jArray[18].Value<double>(),
            AuxiliaryLimitPrice = jArray[19].Value<double>(),
            Hidden = jArray[23].Value<int>() == 1,
            PlacedId = jArray[24].Value<long>(),
            Routing = jArray[28].Value<string>(),
            Meta = jArray[31].ToObject<BitfinexMeta>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, BitfinexOrder value, JsonSerializer serializer) { }
}