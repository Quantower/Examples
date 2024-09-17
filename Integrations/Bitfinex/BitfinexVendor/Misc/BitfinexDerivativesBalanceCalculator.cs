// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Bitfinex.API.Models;
using BitfinexVendor.Extensions;
using TradingPlatform.BusinessLayer;

namespace BitfinexVendor.Misc;

public class BitfinexDerivativesBalanceCalculator : MarginBasedBalanceCalculator
{
    private readonly BitfinexContext bitfinexContext;
    private SettingItem leverageItem;

    public BitfinexDerivativesBalanceCalculator(BitfinexContext bitfinexContext)
    {
        this.bitfinexContext = bitfinexContext;
    }

    protected override void PopulateAction(SettingItem[] orderSettings, OrderRequestParameters requestParameters)
    {
        this.leverageItem = orderSettings?.GetItemByName(BitfinexVendor.LEVERAGE);

        base.PopulateAction(orderSettings, requestParameters);
    }

    protected override void OnOrderSettingChanged(string settingName)
    {
        switch (settingName)
        {
            case BitfinexVendor.LEVERAGE:
                this.UpdateTotalLink();
                this.Recalculate();
                break;
            default:
                base.OnOrderSettingChanged(settingName);
                break;
        }
    }

    protected override double? GetAvailableForOrder()
    {
        if (this.CurrentSymbol == null)
            return null;

        var walletKey = new BitfinexWalletKey(BitfinexWalletType.MARGIN, this.CurrentSymbol.QuotingCurrency.Id);
        if (!this.bitfinexContext.Wallets.TryGetValue(walletKey, out var wallet))
            return null;

        return wallet.AvailableBalance.ToDouble();
    }

    protected override int? GetLeverage() => this.leverageItem?.GetValue<int>() ?? 1;
}