// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using BitfinexVendor.API.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BitfinexVendor.API
{
    class BitfinexRestApi
    {
        private string baseEndpoint;

        public BitfinexRestApi(string baseEndpoint)
        {
            this.baseEndpoint = baseEndpoint;
        }

        public async Task<BitfinexSymbol[]> GetSymbolsDetails() =>
            await MakeRequestAsync<BitfinexSymbol[]>(1, "symbols_details");

        public async Task<BitfinexTrade[]> GetTrades(string symbol, long from, long to) =>
            await MakeRequestAsync<BitfinexTrade[]>(2, $"trades/t{symbol}/hist?start={from}&end={to}&sort=-1&limit={BitfinexConsts.TRADES_LIMIT}");

        public async Task<BitfinexCandle[]> GetCandles(string symbol, string timeFrame, long from, long to) =>
            await MakeRequestAsync<BitfinexCandle[]>(2, $"candles/trade:{timeFrame}:t{symbol}/hist?start={from}&end={to}&sort=-1&limit={BitfinexConsts.CANDLES_LIMIT}");

        private async Task<T> MakeRequestAsync<T>(int version, string endpoint)
        {
            T bitfinexResponse = default;

            var uri = new Uri($"{this.baseEndpoint}/v{version}/{endpoint}");
            var request = WebRequest.CreateHttp(uri);
            var response = await request.GetResponseAsync();

            using (var stream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    bitfinexResponse = JsonConvert.DeserializeObject<T>(json);
                }
            }

            return bitfinexResponse;
        }
    }
}