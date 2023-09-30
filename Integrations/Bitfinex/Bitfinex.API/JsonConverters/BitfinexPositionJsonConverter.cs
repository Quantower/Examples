// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters;

internal class BitfinexPositionJsonConverter : JsonConverter<BitfinexPosition>
{
    public override BitfinexPosition ReadJson(JsonReader reader, Type objectType, BitfinexPosition existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        long? creationTimeTicks = jArray[12].Value<long?>();
        long? updateTimeTicks = jArray[13].Value<long?>();

        var result = new BitfinexPosition
        {
            Symbol = jArray[0].Value<string>(),
            Status = jArray[1].Value<string>(),
            Amount = jArray[2].Value<double>(),
            BasePrice = jArray[3].Value<double>(),
            FundingAmount = jArray[4].Value<double>(),
            FundingType = jArray[5].Value<int>(),
            PnL = jArray[6].Value<double?>(),
            PnLPercentage = jArray[7].Value<double?>(),
            LiquidationPrice = jArray[8].Value<double?>(),
            Leverage = jArray[9].Value<double?>(),
            Id = jArray[11].Value<long>(),
            CreationTime = creationTimeTicks != null ? DateTimeOffset.FromUnixTimeMilliseconds(creationTimeTicks.Value).UtcDateTime : null,
            UpdateTime = updateTimeTicks != null ? DateTimeOffset.FromUnixTimeMilliseconds(updateTimeTicks.Value).UtcDateTime : null,
            Type = jArray[15].Value<int>(),
            Collateral = jArray[17].Value<double?>(),
            MinCollateral = jArray[18].Value<double?>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, BitfinexPosition value, JsonSerializer serializer) { }
}