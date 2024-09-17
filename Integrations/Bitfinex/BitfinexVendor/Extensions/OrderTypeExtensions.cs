// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Bitfinex.API.Models;
using TradingPlatform.BusinessLayer;

namespace BitfinexVendor.Extensions;

internal static class OrderTypeExtensions
{
    public static ValidateResult ValidateIfMarginAllowed(this OrderType orderType, OrderRequestParameters parameters)
    {
        if (parameters.Account.Id.Contains(BitfinexWalletType.MARGIN))
        {
            bool allowMargin = parameters.Symbol.AdditionalInfo.TryGetItem(BitfinexVendor.ALLOW_MARGIN, out var item) && (bool)item.Value;

            if (!allowMargin)
                return ValidateResult.NotValid($"Margin trading is not allowed for {parameters.Symbol.Name}. Please, choose another symbol or not margin account");
        }
        else
        {
            if (parameters.Symbol.SymbolType == SymbolType.Swap)
                return ValidateResult.NotValid("Invalid account for derivatives trading. Margin account should be chosen");
        }

        return ValidateResult.Valid;
    }
}