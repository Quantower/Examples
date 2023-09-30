// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Bitfinex.API.Models;
using Bitfinex.API.Models.Requests;
using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace Bitfinex.API.Abstractions;

public interface IBitfinexPublicRestApiV2
{
    [Get("/candles/trade:{timeFrame}:{symbol}/hist?start={start}&end={end}&limit=10000&sort=-1")]
    Task<BitfinexCandle[]> GetCandles(string symbol, string timeFrame, long start, long end, CancellationToken cancellation);

    [Get("/tickers?symbols=ALL")]
    Task<BitfinexTicker[]> GetTickers(CancellationToken cancellation);

    [Get("/trades/{symbol}/hist?start={start}&end={end}&limit=10000&sort=-1")]
    Task<BitfinexTrade[]> GetTrades(string symbol, long start, long end, CancellationToken cancellation);

    [Get("/conf/pub:{action}:{object}:{detail}")]
    Task<T> GetConfigs<T>(string action, string @object, string detail, CancellationToken cancellation);

    [Get("/status/deriv?keys=ALL")]
    Task<BitfinexDerivativeStatus[]> GetDerivativesStatus(CancellationToken cancellation);

    [Get("/status/deriv/{symbol}/hist")]
    Task<BitfinexDerivativeStatus[]> GetDerivativesStatusHistory(string symbol, [Query] BitfinexDerivativeStatusHistoryRequest request, CancellationToken cancellation);
}