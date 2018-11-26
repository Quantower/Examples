using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace HitBTCVendor
{
    public class HitBTCVendor : Vendor
    {
        #region Consts
        internal const string VENDOR_NAME = "HitBTC";
        internal const string PARAMETER_API_KEY = "apiKey";
        internal const string PARAMETER_SECRET_KEY = "secretKey";

        private const string CONNECTION_INFO = "Info";
        private const string CONNECTION_TRADING = "Trading";
        #endregion Consts

        #region Properties
        private Vendor vendor;
        #endregion Properties

        public HitBTCVendor()
        { }

        #region Integration details
        public override VendorMetaData GetVendorMetaData() => new VendorMetaData
        {
            VendorName = VENDOR_NAME,
            VendorDescription = "Market data connection. Trading coming soon."
        };
        #endregion Integration details

        #region Connection
        public override IList<SettingItem> GetConnectionParameters()
        {
            var infoItem = new SelectItem(CONNECTION_INFO, CONNECTION_INFO);
            var tradingItem = new SelectItem(CONNECTION_TRADING, CONNECTION_TRADING);

            var relation = new SettingItemRelationEnability(CONNECTION, tradingItem);

            var result =  new List<SettingItem>
            {
                new SettingItemGroup(LOGIN_PARAMETER_GROUP, new List<SettingItem>
                {
                    new SettingItemRadioLocalized(CONNECTION, infoItem, new List<SelectItem> { infoItem, tradingItem }),
                    new SettingItemString(PARAMETER_API_KEY, string.Empty)
                    {
                        Text = "API key",
                        Relation = relation
                    },
                    new SettingItemPassword(PARAMETER_SECRET_KEY, new PasswordHolder())
                    {
                        Text = "Secret key",
                        Relation = relation
                    }
                })
            };

            return result;
        }

        public override IList<ConnectionInfo> GetDefaultConnections() => new List<ConnectionInfo>
        {
            this.CreateDefaultConnectionInfo("HitBTC", VENDOR_NAME, "HitBTCVendor\\hit_btc.svg")
        };

        public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return ConnectionResult.CreateFail("Network does not available");

            var settingItem = connectRequestParameters.ConnectionSettings.GetItemByPath(LOGIN_PARAMETER_GROUP, CONNECTION);
            if (settingItem == null || !(settingItem.Value is SelectItem selectItem))
                return ConnectionResult.CreateFail("Can't find connection parameters");

            MarketDataVendor hitBTCVendor = null;
            if (selectItem.Value.ToString() == CONNECTION_INFO)
                hitBTCVendor = new MarketDataVendor();
            else
                hitBTCVendor = new TradingVendor();

            hitBTCVendor.NewMessage += (m) => this.PushMessage(m);

            this.vendor = hitBTCVendor;

            return this.vendor.Connect(connectRequestParameters);
        }

        public override void Disconnect() => this.vendor.Disconnect();

        public override void OnConnected(CancellationToken token) => this.vendor.OnConnected(token);

        public override PingResult Ping() => this.vendor.Ping();

        #endregion Connection

        #region Symbols and symbol groups         
        public override IList<MessageSymbol> GetSymbols() => this.vendor.GetSymbols();

        public override MessageSymbolTypes GetSymbolTypes() => this.vendor.GetSymbolTypes();

        public override IList<MessageAsset> GetAssets() => this.vendor.GetAssets();

        public override IList<MessageExchange> GetExchanges() => this.vendor.GetExchanges();
        #endregion

        #region Accounts and rules
        public override IList<MessageAccount> GetAccounts() => this.vendor.GetAccounts();

        public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances() => this.vendor.GetCryptoAssetBalances();

        public override IList<MessageRule> GetRules() => this.vendor.GetRules();
        #endregion Accounts and rules

        #region Subscriptions
        public override void SubscribeSymbol(SubscribeQuotesParameters parameters) => this.vendor.SubscribeSymbol(parameters);

        public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters) => this.vendor.UnSubscribeSymbol(parameters);
        #endregion Subscriptions

        #region History
        public override HistoryMetadata GetHistoryMetadata() => this.vendor.GetHistoryMetadata();

        public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters) => this.vendor.LoadHistory(requestParameters);
        #endregion History

        #region Orders
        public override IList<OrderType> GetAllowedOrderTypes() => this.vendor.GetAllowedOrderTypes();

        public override IList<MessageOpenOrder> GetPendingOrders() => this.vendor.GetPendingOrders();
        #endregion Orders

        #region Trading
        public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters parameters) => this.vendor.PlaceOrder(parameters);

        public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters parameters) => this.vendor.ModifyOrder(parameters);

        public override TradingOperationResult CancelOrder(CancelOrderRequestParameters parameters) => this.vendor.CancelOrder(parameters);
        #endregion Trading

        #region Reports
        public override IList<MessageReportType> GetReportsMetaData() => this.vendor.GetReportsMetaData();

        public override Report GenerateReport(ReportRequestParameters reportRequestParameters) => this.vendor.GenerateReport(reportRequestParameters);
        #endregion Reports
    }
}