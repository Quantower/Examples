// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexAccountSummaryJsonConverter : JsonConverter<BitfinexAccountSummary>
    {
        public override BitfinexAccountSummary ReadJson(JsonReader reader, Type objectType, BitfinexAccountSummary existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var feeArray = jArray[4].Value<JArray>();

            var makerFeeArray = feeArray[0].Value<JArray>();
            var takerFeeArray = feeArray[1].Value<JArray>();

            var result = new BitfinexAccountSummary()
            {
                MakerFee = makerFeeArray[0].Value<double>(),
                DerivativeRebate = makerFeeArray[1].Value<double>(),
                TakerFeeToCrypto = takerFeeArray[0].Value<double>(),
                TakerFeeToStable = takerFeeArray[1].Value<double>(),
                TakerFeeToFiat = takerFeeArray[2].Value<double>(),
                DerivativeTakerFee = takerFeeArray[5].Value<double>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexAccountSummary value, JsonSerializer serializer)
        { }
    }
}