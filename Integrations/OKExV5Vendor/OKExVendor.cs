using OKExV5Vendor.Market;
using OKExV5Vendor.Trading;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace OKExV5Vendor
{
    public class OKExVendor : Vendor
    {
        private Vendor currentVendor;

        static OKExVendor()
        {

        }

        #region Integration details

        public static VendorMetaData GetVendorMetaData() => new VendorMetaData
        {
            VendorName = OKExConsts.VENDOR_NAME,
            GetDefaultConnections = () => new List<ConnectionInfo>
            {
                CreateDefaultConnectionInfo(OKExConsts.VENDOR_NAME, OKExConsts.VENDOR_NAME, Path.Combine("OKExV5Vendor", "okex.svg"),null,  true,links:new List<ConnectionInfoLink>()
                    {
                        new ConnectionInfoLink()
                        {
                            Text = "Register account",
                            URL = @"https://www.okex.com/join/8738452"
                        }
                    })
            },
            GetConnectionParameters = () =>
            {
                var infoItem = new SelectItem(OKExConsts.CONNECTION_INFO, OKExConsts.CONNECTION_INFO);
                var tradingItem = new SelectItem(OKExConsts.CONNECTION_TRADING, OKExConsts.CONNECTION_TRADING);

                var relation = new SettingItemRelationEnability(CONNECTION, tradingItem);

                return new List<SettingItem>
                {
                    new SettingItemGroup(LOGIN_PARAMETER_GROUP,
                        new List<SettingItem>
                        {
                            new SettingItemRadioLocalized(CONNECTION, infoItem, new List<SelectItem> { infoItem, tradingItem }),
                            new SettingItemString(OKExConsts.PARAMETER_API_KEY, string.Empty)
                            {
                                Text = loc.key("API key"),
                                Relation = relation
                            },
                            new SettingItemPassword(OKExConsts.PARAMETER_SECRET_ID, new PasswordHolder())
                            {
                                Text = loc.key("Secret key"),
                                Relation = relation
                            },
                            new SettingItemPassword(OKExConsts.PARAMETER_PASSPHRASE_ID, new PasswordHolder())
                            {
                                Text = loc.key("Password"),
                                Relation = relation
                            }
                        })
                };
            }
        };

        #endregion Integration details

        #region Connection

        public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return ConnectionResult.CreateFail(loc._("Network does not available"));

            var settingItem = connectRequestParameters.ConnectionSettings.GetItemByPath(LOGIN_PARAMETER_GROUP, CONNECTION);
            if (settingItem == null || settingItem.Value is not SelectItem connection)
                return ConnectionResult.CreateFail(loc._("Can't find connection parameters"));

            switch (connection.Value)
            {
                case OKExConsts.CONNECTION_INFO:
                    {
                        var client = new OKExMarketClient(OKExConsts.RealServer);
                        this.currentVendor = new OKExMarketVendor(client);
                        break;
                    }
                case OKExConsts.CONNECTION_TRADING:
                    {
                        var parameters = connectRequestParameters.ConnectionSettings;
                        var apikey = string.Empty;
                        var secretId = string.Empty;
                        var passphase = string.Empty;

                        // apikey
                        settingItem = parameters.GetItemByPath(LOGIN_PARAMETER_GROUP, OKExConsts.PARAMETER_API_KEY);
                        if (settingItem?.Value != null)
                            apikey = settingItem.Value.ToString();

                        if (string.IsNullOrEmpty(apikey))
                            return ConnectionResult.CreateFail(loc._("ApiKey is empty"));

                        // secret id
                        settingItem = parameters.GetItemByPath(LOGIN_PARAMETER_GROUP, OKExConsts.PARAMETER_SECRET_ID);
                        if (settingItem?.Value != null)
                            secretId = ((PasswordHolder)settingItem.Value)?.Password?.ToString();

                        if (string.IsNullOrEmpty(secretId))
                            return ConnectionResult.CreateFail(loc._("Secret id is empty"));

                        // passphase
                        settingItem = parameters.GetItemByPath(LOGIN_PARAMETER_GROUP, OKExConsts.PARAMETER_PASSPHRASE_ID);
                        if (settingItem?.Value != null)
                            passphase = ((PasswordHolder)settingItem.Value)?.Password?.ToString();

                        if (string.IsNullOrEmpty(secretId))
                            return ConnectionResult.CreateFail(loc._("Passphase is empty"));

                        var client = new OKExTradingClient(apikey, secretId, passphase, OKExConsts.RealServer);
                        this.currentVendor = new OKExTradingVendor(client);

                        break;
                    }
            }

            this.currentVendor.NewMessage += this.CurrentVendor_NewMessage;
            return this.currentVendor?.Connect(connectRequestParameters);
        }
        public override void OnConnected(CancellationToken token) => this.currentVendor?.OnConnected(token);
        public override void Disconnect()
        {
            if (this.currentVendor != null)
            {
                this.currentVendor.Disconnect();
                this.currentVendor.NewMessage -= this.CurrentVendor_NewMessage;
            }

            base.Disconnect();
        }
        public override PingResult Ping() => this.currentVendor?.Ping();
        private void CurrentVendor_NewMessage(object sender, VendorEventArgs e) => this.PushMessage(e.Message);

        #endregion Connection

        #region Accounts and rules

        public override IList<MessageRule> GetRules(CancellationToken token) => this.currentVendor.GetRules(token);
        public override IList<MessageAccount> GetAccounts(CancellationToken token) => this.currentVendor.GetAccounts(token);
        public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances(CancellationToken token) => this.currentVendor.GetCryptoAssetBalances(token);

        #endregion Accounts and rules

        #region Symbols and symbol groups

        public override IList<MessageSymbol> GetSymbols(CancellationToken token) => this.currentVendor?.GetSymbols(token);
        public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => this.currentVendor?.GetSymbolTypes(token);
        public override IList<MessageAsset> GetAssets(CancellationToken token) => this.currentVendor?.GetAssets(token);
        public override IList<MessageExchange> GetExchanges(CancellationToken token) => this.currentVendor?.GetExchanges(token);
        public override bool AllowNonFixedList => true;
        public override IList<MessageSymbolInfo> GetFutureContracts(GetFutureContractsRequestParameters requestParameters) => this.currentVendor?.GetFutureContracts(requestParameters);
        public override MessageSymbol GetNonFixedSymbol(GetSymbolRequestParameters requestParameters) => this.currentVendor?.GetNonFixedSymbol(requestParameters);
        public override IList<MessageSymbolInfo> SearchSymbols(SearchSymbolsRequestParameters requestParameters) => this.currentVendor?.SearchSymbols(requestParameters);
        public override IList<MessageSymbolInfo> GetStrikes(GetStrikesRequestParameters requestParameters) => this.currentVendor?.GetStrikes(requestParameters);
        public override IList<MessageOptionSerie> GetOptionSeries(GetOptionSeriesRequestParameters requestParameters) => this.currentVendor?.GetOptionSeries(requestParameters);

        #endregion Symbols and symbol groups

        #region Positions and Orders

        public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token) => this.currentVendor?.GetAllowedOrderTypes(token);
        public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token) => this.currentVendor?.GetPendingOrders(token);
        public override IList<MessageOpenPosition> GetPositions(CancellationToken token) => this.currentVendor?.GetPositions(token);
        public override PnL CalculatePnL(PnLRequestParameters parameters) => this.currentVendor?.CalculatePnL(parameters);

        #endregion Positions and Orders

        #region Trading opertions

        public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters parameters) => this.currentVendor?.PlaceOrder(parameters);
        public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters parameters) => this.currentVendor?.ModifyOrder(parameters);
        public override TradingOperationResult CancelOrder(CancelOrderRequestParameters parameters) => this.currentVendor?.CancelOrder(parameters);
        public override TradingOperationResult ClosePosition(ClosePositionRequestParameters parameters) => this.currentVendor?.ClosePosition(parameters);

        #endregion Trading opertions

        #region Subscription

        public override void SubscribeSymbol(SubscribeQuotesParameters parameters) => this.currentVendor?.SubscribeSymbol(parameters);
        public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters) => this.currentVendor?.UnSubscribeSymbol(parameters);

        #endregion Subscribtion

        #region History

        public override HistoryMetadata GetHistoryMetadata(CancellationToken cancelationToken) => this.currentVendor?.GetHistoryMetadata(cancelationToken);
        public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters) => this.currentVendor?.LoadHistory(requestParameters);

        #endregion History

        #region Reports

        public override IList<MessageReportType> GetReportsMetaData(CancellationToken token) => this.currentVendor?.GetReportsMetaData(token);
        public override Report GenerateReport(ReportRequestParameters reportRequestParameters) => this.currentVendor?.GenerateReport(reportRequestParameters);

        #endregion Reports

        #region Trades history

        public override TradesHistoryMetadata GetTradesMetadata() => this.currentVendor?.GetTradesMetadata();

        public override IList<MessageTrade> GetTrades(TradesHistoryRequestParameters parameters) => this.currentVendor?.GetTrades(parameters);

        #endregion Trades history

        #region Order history

        public override IList<MessageOrderHistory> GetOrdersHistory(OrdersHistoryRequestParameters parameters) => this.currentVendor?.GetOrdersHistory(parameters);

        #endregion Order history
    }
}
