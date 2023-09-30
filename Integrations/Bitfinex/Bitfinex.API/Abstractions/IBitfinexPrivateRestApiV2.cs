// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Bitfinex.API.Models;
using Bitfinex.API.Models.Requests;
using Refit;

namespace Bitfinex.API.Abstractions;

public interface IBitfinexPrivateRestApiV2
{
    [Post("/auth/r/wallets")]
    Task<BitfinexWallet[]> GetWallets(CancellationToken cancellation);

    [Post("/auth/r/info/margin/{key}")]
    Task<T> GetMarginInfo<T>(string key, CancellationToken cancellation);

    [Post("/auth/r/orders")]
    Task<BitfinexOrder[]> GetActiveOrders(CancellationToken cancellation);

    [Post("/auth/w/order/submit")]
    Task<BitfinexOrderResponse> SubmitOrder([Body] BitfinexSubmitOrderRequest request, CancellationToken cancellation);

    [Post("/auth/w/order/update")]
    Task<BitfinexOrderResponse> UpdateOrder([Body] BitfinexUpdateOrderRequest request, CancellationToken cancellation);

    [Post("/auth/w/order/cancel")]
    Task<BitfinexOrderResponse> CancelOrder([Body] BitfinexCancelOrderRequest request, CancellationToken cancellation);

    [Post("/auth/r/positions")]
    Task<BitfinexPosition[]> GetPositions(CancellationToken cancellation);

    [Post("/auth/r/trades/hist")]
    Task<BitfinexUserTrade[]> GetUserTrades([Body] BitfinexUserTradesRequest request, CancellationToken cancellation);

    [Post("/auth/r/orders/hist")]
    Task<BitfinexOrder[]> GetOrdersHistory([Body] BitfinexOrdersHistoryRequest request, CancellationToken cancellation);

    [Post("/auth/calc/order/avail")]
    Task<double[]> GetAvailableBalance([Body] BitfinexAvailableBalanceRequest request, CancellationToken cancellation);

    [Post("/auth/r/info/user")]
    Task<BitfinexUserInfo> GetUserInfo(CancellationToken cancellation);

    [Post("/auth/r/summary")]
    Task<BitfinexAccountSummary> GetAccountSummary(CancellationToken cancellation);
}