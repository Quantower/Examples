// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Bitfinex.API.Models;
using BitfinexVendor.Misc;

namespace BitfinexVendor.Extensions;

internal static class BitfinexWalletExtensions
{
    public static BitfinexWalletKey GetKey(this BitfinexWallet wallet) => new(wallet.Type, wallet.Currency);
}