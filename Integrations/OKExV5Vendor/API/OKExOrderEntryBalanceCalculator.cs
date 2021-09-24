using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.OrderType;
using OKExV5Vendor.API.REST.Models;
using TradingPlatform.BusinessLayer;

namespace OKExV5Vendor.API
{
    class OKExOrderEntryBalanceCalculator : RegularBalanceCalculator
    {
        #region Properties

        private readonly IOKExOrderEntryDataProvider dataProvider;

        private protected Symbol CurrentSymbol
        {
            get => this.currentSymbol;
            set
            {
                if (this.currentSymbol == value)
                    return;

                if (this.currentSymbol != null)
                    this.currentSymbol.Updated -= this.CurrentSymbolOnUpdated;

                this.currentSymbol = value;

                if (this.currentSymbol == null)
                {
                    this.IsSpotSymbol = false;
                    this.IsContractBasedSymbol = false;
                }

                if (this.currentSymbol != null)
                {
                    this.currentSymbol.Updated += this.CurrentSymbolOnUpdated;
                    this.okexSymbol = this.dataProvider.GetSymbol(value.Id);

                    this.IsContractBasedSymbol = this.okexSymbol.ContractType != OKExContractType.Undefined;
                    this.IsSpotSymbol = this.okexSymbol.InstrumentType == OKExInstrumentType.Spot;


                    //
                    if (this.okexSymbol != null)
                        this.dataProvider.PopulateLeverage(this.okexSymbol);
                }

                this.RepopulateTotalAndBalanceAssets();
            }
        }
        private Symbol currentSymbol;
        private OKExSymbol okexSymbol;

        private Account CurrentAccount
        {
            get => this.currentAccount;
            set
            {
                if (this.currentAccount == value)
                    return;

                if (this.currentAccount != null)
                    this.currentAccount.Updated -= this.CurrentAccountOnUpdated;

                this.currentAccount = value;

                if (this.currentAccount != null)
                    this.currentAccount.Updated += this.CurrentAccountOnUpdated;
            }
        }
        private Account currentAccount;

        private SettingItemSelectorLocalized tradeMode;
        private OKExTradeMode? SelectedTradeMode
        {
            get
            {
                if (this.tradeMode?.Value == null)
                    return null;

                return (OKExTradeMode)((SelectItem)this.tradeMode.Value).Value;
            }
        }

        private SettingItemSelector marginCurrency;
        protected SettingItemSelector MarginCurrency
        {
            get => this.marginCurrency;
            private set
            {
                if (this.marginCurrency != null)
                    this.marginCurrency.PropertyChanged -= this.MarginCurrency_PropertyChanged;

                this.marginCurrency = value;

                if (this.marginCurrency != null)
                    this.marginCurrency.PropertyChanged += this.MarginCurrency_PropertyChanged;
            }
        }

        private SettingItemDouble quantitySI;

        protected bool IsSpotSymbol { get; private set; }
        protected bool IsContractBasedSymbol { get; set; }
        protected bool IsMarketBasedOrder => this.RequestParameters?.OrderTypeId != null && (this.RequestParameters.OrderTypeId == TradingPlatform.BusinessLayer.OrderType.Market || this.RequestParameters.OrderTypeId == TradingPlatform.BusinessLayer.OrderType.Stop || this.RequestParameters.OrderTypeId == OKExTriggerMarketOrderType.ID);

        protected Asset TotalAsset { get; private set; }
        protected Asset BalanceAsset { get; private set; }
        protected Asset SelectedMarginCurrency
        {
            get
            {
                if (this.CurrentSymbol != null)
                {
                    if (this.MarginCurrency?.Value is string assetId)
                    {
                        if (this.CurrentSymbol.Product.Id == assetId)
                            return this.CurrentSymbol.Product;
                        else if (this.CurrentSymbol.QuotingCurrency.Id == assetId)
                            return this.CurrentSymbol.QuotingCurrency;
                    }
                }

                return null;
            }
        }

        #endregion Properties

        public OKExOrderEntryBalanceCalculator(IOKExOrderEntryDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        #region Base overrides

        public override void Populate(SettingItem[] orderSettings, OrderRequestParameters requestParameters)
        {
            base.Populate(orderSettings, requestParameters);

            this.tradeMode = orderSettings.GetItemByName(OKExOrderTypeHelper.TRADE_MODE_TYPE) as SettingItemSelectorLocalized;
            this.MarginCurrency = orderSettings.GetItemByName(OKExOrderTypeHelper.MARGIN_CURRENCY) as SettingItemSelector;

            this.quantitySI = orderSettings?.GetItemByName(TradingPlatform.BusinessLayer.OrderType.QUANTITY) as SettingItemDouble;
            this.PopulateQuantityDimension();

            this.CurrentSymbol = requestParameters.Symbol;
            this.CurrentAccount = requestParameters.Account;

            this.RepopulateTotalAndBalanceAssets();

            this.PopulateTotal(requestParameters);
            this.UpdateTotal(this.GetQuantity());
            this.UpdateTotalLink();
        }
        public override void Dispose()
        {
            this.quantitySI = null;
            base.Dispose();
        }
        protected override Asset GetTotalAsset() => this.TotalAsset;
        protected override double CalculateQuantity(double total, double fillPrice)
        {
            if (this.IsSpotSymbol)
            {
                if (this.TotalAsset == this.CurrentSymbol.Product)
                    return total * this.CurrentSymbol.Last;
                else
                    return total / this.CurrentSymbol.Last;
            }
            else if (this.IsContractBasedSymbol)
            {
                if (this.okexSymbol.ContractType == OKExContractType.Linear)
                    return total / this.CurrentSymbol.Last / this.okexSymbol.ContractValue.Value;
                else
                    return total * this.CurrentSymbol.Last / this.okexSymbol.ContractValue.Value;
            }

            return base.CalculateQuantity(total, fillPrice);
        }
        protected override double CalculateTotal(double quantity, double fillPrice)
        {
            if (this.CurrentSymbol != null && this.okexSymbol != null)
            {
                // spot
                if (this.IsSpotSymbol)
                {
                    if (this.TotalAsset == this.CurrentSymbol.Product)
                        return quantity / this.CurrentSymbol.Last;
                    else
                        return quantity * this.CurrentSymbol.Last;
                }
                // futures/swap
                else if (this.IsContractBasedSymbol)
                {
                    if (this.okexSymbol.ContractType == OKExContractType.Linear)
                        return quantity * this.okexSymbol.ContractValue.Value * this.CurrentSymbol.Last;
                    else
                        return quantity * this.okexSymbol.ContractValue.Value / this.CurrentSymbol.Last;
                }
            }

            return base.CalculateTotal(quantity, fillPrice);
        }
        protected override ulong CalculateSliderStep()
        {
            if (this.GetAvailableBalanceWithLeverage() is double availableBalance)
            {
                if (availableBalance == 0d)
                    availableBalance = 1d;

                if (this.TotalAsset == this.BalanceAsset)
                {
                    if (this.GetTotal() is var total)
                        return (ulong)(total * 100 * TradingPlatform.BusinessLayer.OrderType.BALANCE_PERCENT_STEPS_COUNT_MULTIPLIER / availableBalance);
                }
                else
                {
                    if (this.GetQuantity() is var quantity)
                        return (ulong)(quantity * 100 * TradingPlatform.BusinessLayer.OrderType.BALANCE_PERCENT_STEPS_COUNT_MULTIPLIER / availableBalance);
                }
            }

            return base.CalculateSliderStep();
        }
        protected override string GetTotalLinkText()
        {
            if (this.GetAvailableBalanceWithLeverage() is not double availableBalance)
                return base.GetTotalLinkText();

            return this.BalanceAsset?.FormatPriceWithCurrency(availableBalance);
        }
        protected override void OnLinkAction(object obj)
        {
            if (this.GetAvailableBalanceWithLeverage() is double availableBalance)
            {
                if (this.IsSpotSymbol)
                {
                    if (this.TotalAsset == this.BalanceAsset)
                        this.UpdateQuantity(availableBalance);
                    else
                        this.UpdateTotal(availableBalance);
                }
                else if (this.IsContractBasedSymbol)
                {
                    var coinsPerContract = this.okexSymbol.ContractType == OKExContractType.Linear
                        ? this.okexSymbol.ContractValue.Value * this.CurrentSymbol.Last
                        : this.okexSymbol.ContractValue.Value / this.CurrentSymbol.Last;

                    this.UpdateQuantity((int)(availableBalance / coinsPerContract) * coinsPerContract);
                }
            }
        }
        protected override void OnSideChanged()
        {
            if (this.IsSpotSymbol)
            {
                this.RepopulateTotalAndBalanceAssets();
                this.PopulateTotal(this.RequestParameters);
                this.UpdateTotalLink();
            }
        }
        protected override void OnPercentChanged()
        {
            if (this.GetAvailableBalanceWithLeverage() is double availableBalance && this.GetSliderPercent() is double sliderPercent)
            {
                if (this.TotalAsset == this.BalanceAsset)
                {
                    double total = availableBalance * sliderPercent / 100;
                    this.UpdateQuantity(total);
                    this.UpdateTotal(this.GetQuantity());
                }
                else
                {
                    var qty = availableBalance * sliderPercent / 100;
                    this.UpdateTotal(qty);
                    this.UpdateQuantity(this.GetTotal());
                }
            }
        }

        #endregion Base overrides

        #region Event handlers

        private void CurrentSymbolOnUpdated(Symbol symbol)
        {
            this.UpdateTotalLink();
        }
        private void CurrentAccountOnUpdated(Account obj)
        {
            this.UpdateTotalLink();
        }
        private void MarginCurrency_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.IsSpotSymbol)
            {
                this.RepopulateTotalAndBalanceAssets();
                this.PopulateTotal(this.RequestParameters);
                this.UpdateTotalLink();
            }
        }

        #endregion Event handlers

        #region Misc

        private double? GetAvailableBalanceWithLeverage()
        {
            if (this.GetRealAvailableEquity() is not double availableEquity)
                return null;

            if (this.GetLeverage() is not int leverage)
                return null;

            return availableEquity * leverage;
        }
        private double? GetRealAvailableEquity()
        {
            if (this.BalanceAsset != null && this.dataProvider.Balances.TryGetValue(this.BalanceAsset.Id, out var balance))
                return balance.AvailableEquity;

            return null;
        }
        private int? GetLeverage()
        {
            var tradeMode = this.SelectedTradeMode;

            if (tradeMode != OKExTradeMode.Cash)
            {
                var dict = tradeMode == OKExTradeMode.Cross
                    ? this.okexSymbol.CrossLeverage
                    : this.okexSymbol.IsolatedLeverage;

                if (this.dataProvider.Account.PositionMode == OKExPositionMode.Net || this.okexSymbol.InstrumentType == OKExInstrumentType.Spot)
                {
                    if (dict.TryGetValue(OKExPositionSide.Net, out int leverItem))
                        return leverItem;
                }
                else
                {
                    if (this.RequestParameters.Side == Side.Buy && dict.TryGetValue(OKExPositionSide.Long, out var leverItem) || this.RequestParameters.Side == Side.Sell && dict.TryGetValue(OKExPositionSide.Short, out leverItem))
                        return leverItem;
                }
            }

            return 1;
        }

        private void RepopulateTotalAndBalanceAssets()
        {
            if (this.IsSpotSymbol)
            {
                switch (this.SelectedTradeMode)
                {
                    case OKExTradeMode.Cash:
                        {
                            if (this.RequestParameters.Side == Side.Buy)
                                this.BalanceAsset = this.CurrentSymbol.QuotingCurrency;
                            else
                                this.BalanceAsset = this.CurrentSymbol.Product;

                            this.TotalAsset = this.CurrentSymbol.QuotingCurrency;

                            break;
                        }
                    case OKExTradeMode.Cross:
                        {
                            if (this.SelectedMarginCurrency != null)
                                this.BalanceAsset = this.SelectedMarginCurrency;

                            if (this.IsMarketBasedOrder && this.RequestParameters.Side == Side.Buy)
                                this.TotalAsset = this.CurrentSymbol.Product;
                            else
                                this.TotalAsset = this.CurrentSymbol.QuotingCurrency;

                            break;
                        }
                    case OKExTradeMode.Isolated:
                        {
                            if (this.RequestParameters.Side == Side.Buy)
                                this.BalanceAsset = this.CurrentSymbol.Product;
                            else
                                this.BalanceAsset = this.CurrentSymbol.QuotingCurrency;

                            if (this.IsMarketBasedOrder && this.RequestParameters.Side == Side.Buy)
                                this.TotalAsset = this.CurrentSymbol.Product;
                            else
                                this.TotalAsset = this.CurrentSymbol.QuotingCurrency;

                            break;
                        }
                }
            }
            else if (this.IsContractBasedSymbol)
            {
                this.TotalAsset = this.CurrentSymbol.Product;
                this.BalanceAsset = this.CurrentSymbol.Product;
            }
            else
            {
                this.TotalAsset = null;
                this.BalanceAsset = null;
            }

            //
            this.PopulateQuantityDimension();
        }
        private void PopulateQuantityDimension()
        {
            if (this.quantitySI == null)
                return;

            if (!this.IsSpotSymbol)
                return;

            if (this.TotalAsset == this.CurrentSymbol.Product)
                this.quantitySI.Dimension = this.CurrentSymbol.QuotingCurrency.Name;
            else
                this.quantitySI.Dimension = this.CurrentSymbol.Product.Name;
        }

        #endregion Misc
    }
}
