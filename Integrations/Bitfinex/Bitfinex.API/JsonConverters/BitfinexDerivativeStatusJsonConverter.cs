// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexDerivativeStatusJsonConverter : JsonConverter<BitfinexDerivativeStatus>
    {
        public override BitfinexDerivativeStatus ReadJson(JsonReader reader, Type objectType, BitfinexDerivativeStatus existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = jArray.Count == 24 ?
                new BitfinexDerivativeStatus
            {
                Symbol = jArray[0].Value<string>(),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(jArray[1].Value<long>()).UtcDateTime,
                Price = jArray[3].Value<double?>(),
                SpotPrice = jArray[4].Value<double?>(),
                InsuranceFundBalance = jArray[6].Value<double?>(),
                NextFundingTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(jArray[8].Value<long>()).UtcDateTime,
                NextFundingAccrued = jArray[9].Value<double?>(),
                NextFundingStep = jArray[10].Value<long?>(),
                CurrentFunding = jArray[12].Value<double?>(),
                MarkPrice = jArray[15].Value<double?>(),
                OpenInterest = jArray[18].Value<double?>(),
                ClampMin = jArray[22].Value<double?>(),
                ClampMax = jArray[23].Value<double?>()
            } : new BitfinexDerivativeStatus
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(jArray[0].Value<long>()).UtcDateTime,
                Price = jArray[2].Value<double?>(),
                SpotPrice = jArray[3].Value<double?>(),
                InsuranceFundBalance = jArray[5].Value<double?>(),
                NextFundingTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(jArray[7].Value<long>()).UtcDateTime,
                NextFundingAccrued = jArray[8].Value<double?>(),
                NextFundingStep = jArray[9].Value<long?>(),
                CurrentFunding = jArray[11].Value<double?>(),
                MarkPrice = jArray[14].Value<double?>(),
                OpenInterest = jArray[17].Value<double?>(),
                ClampMin = jArray[21].Value<double?>(),
                ClampMax = jArray[22].Value<double?>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexDerivativeStatus value, JsonSerializer serializer)
        { }
    }
}