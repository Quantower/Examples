// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using TradingPlatform.BusinessLayer;

namespace BitfinexVendor.Misc;

public class BitfinexMarginBalanceCalculator : MarginBasedBalanceCalculator
{
    private readonly BitfinexContext bitfinexContext;

    private SettingItem sideItem;

    public BitfinexMarginBalanceCalculator(BitfinexContext bitfinexContext)
    {
        this.bitfinexContext = bitfinexContext;
    }

    protected override void PopulateAction(SettingItem[] orderSettings, OrderRequestParameters requestParameters)
    {
        this.sideItem = orderSettings?.GetItemByName(OrderType.SIDE);

        base.PopulateAction(orderSettings, requestParameters);
    }

    protected override void OnSideChanged()
    {
        this.UpdateTotalLink();
        this.Recalculate();

        base.OnSideChanged();
    }

    protected override double? GetAvailableForOrder()
    {
        if (this.CurrentSymbol == null)
            return null;

        if (!this.bitfinexContext.SymbolsMarginInfo.TryGetValue(this.CurrentSymbol.Id, out var marginInfo))
            return null;

        var side = this.GetSide();

        if (side == null)
            return (double?)marginInfo.TradableBalance;

        if (this.CurrentFillPrice == null)
            return null;

        if (marginInfo.Buy == null || marginInfo.Sell == null)
            return null;

        double marketPrice = side.Value == Side.Buy ? this.CurrentSymbol.Ask : this.CurrentSymbol.Bid;
        double availableForMarket = (double)(side.Value == Side.Buy ? marginInfo.Buy : marginInfo.Sell);
        double orderPrice = this.CurrentFillPrice.Value;

        double availableForOrder = availableForMarket / (orderPrice / marketPrice);
        double availableForOrderInQuoteCurrency = availableForOrder * orderPrice;

        return availableForOrderInQuoteCurrency;
    }

    protected override int? GetLeverage() => 1;

    private Side? GetSide() => (Side?)this.sideItem?.GetValue<int>();
}