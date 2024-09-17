// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Bitfinex.API.Models;
using Refit;

namespace Bitfinex.API.Abstractions;

public interface IBitfinexRestApiV1
{
    [Get("/symbols_details")]
    Task<BitfinexSymbolDetails[]> GetSymbolDetails(CancellationToken cancellation);
}