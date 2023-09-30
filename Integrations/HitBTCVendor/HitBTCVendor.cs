// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using SertificateValidatorShared;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace HitBTCVendor;

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

    static HitBTCVendor()
    {
        SertificateValidator.CheckAssemblyCertificate(typeof(Core).Assembly.Location);
        SertificateValidator.CheckAssemblyCertificate(typeof(HitBTCVendor).Assembly.Location);

        AssembliesSameBuildValidator.CheckIsSameBuild(typeof(Core).Assembly, typeof(HitBTCVendor).Assembly);
    }

    public HitBTCVendor()
    {
    }

    #region Integration details

    public static VendorMetaData GetVendorMetaData() => new VendorMetaData
    {
        VendorName = VENDOR_NAME,
        VendorDescription = loc.key("Market data connection. Trading coming soon."),

        GetDefaultConnections = () => new List<ConnectionInfo>
        {
            CreateDefaultConnectionInfo("HitBTC", VENDOR_NAME, "HitBTCVendor\\hit_btc.svg")
        },
        GetConnectionParameters = () =>
        {
            var infoItem = new SelectItem(CONNECTION_INFO, CONNECTION_INFO);
            var tradingItem = new SelectItem(CONNECTION_TRADING, CONNECTION_TRADING);

            var relation = new SettingItemRelationEnability(CONNECTION, tradingItem);

            return new List<SettingItem>
            {
                new SettingItemGroup(LOGIN_PARAMETER_GROUP, new List<SettingItem>
                {
                    new SettingItemRadioLocalized(CONNECTION, infoItem, new List<SelectItem> { infoItem, tradingItem }),
                    new SettingItemString(PARAMETER_API_KEY, string.Empty)
                    {
                        Text = loc.key("API key"),
                        Relation = relation
                    },
                    new SettingItemPassword(PARAMETER_SECRET_KEY, new PasswordHolder())
                    {
                        Text = loc.key("Secret key"),
                        Relation = relation
                    }
                })
            };
        },
    };

    #endregion Integration details

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return ConnectionResult.CreateFail(loc._("Network does not available"));

        var settingItem = connectRequestParameters.ConnectionSettings.GetItemByPath(LOGIN_PARAMETER_GROUP, CONNECTION);
        if (settingItem == null || !(settingItem.Value is SelectItem selectItem))
            return ConnectionResult.CreateFail(loc._("Can't find connection parameters"));

        MarketDataVendor hitBTCVendor = null;
        if (selectItem.Value.ToString() == CONNECTION_INFO)
            hitBTCVendor = new MarketDataVendor();
        else
            hitBTCVendor = new TradingVendor();

        hitBTCVendor.NewMessage += (s, e) => this.PushMessage(e.Message);

        this.vendor = hitBTCVendor;

        return this.vendor.Connect(connectRequestParameters);
    }

    public override void Disconnect() => this.vendor.Disconnect();

    public override void OnConnected(CancellationToken token) => this.vendor.OnConnected(token);

    public override PingResult Ping() => this.vendor.Ping();

    #endregion Connection

    #region Symbols and symbol groups

    public override IList<MessageSymbol> GetSymbols(CancellationToken token) => this.vendor.GetSymbols(token);

    public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => this.vendor.GetSymbolTypes(token);

    public override IList<MessageAsset> GetAssets(CancellationToken token) => this.vendor.GetAssets(token);

    public override IList<MessageExchange> GetExchanges(CancellationToken token) => this.vendor.GetExchanges(token);
    #endregion

    #region Accounts and rules
    public override IList<MessageAccount> GetAccounts(CancellationToken token) => this.vendor.GetAccounts(token);

    public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances(CancellationToken token) => this.vendor.GetCryptoAssetBalances(token);

    public override IList<MessageRule> GetRules(CancellationToken token) => this.vendor.GetRules(token);

    #endregion Accounts and rules

    #region Subscriptions
    public override void SubscribeSymbol(SubscribeQuotesParameters parameters) => this.vendor.SubscribeSymbol(parameters);

    public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters) => this.vendor.UnSubscribeSymbol(parameters);
    #endregion Subscriptions

    #region History
    public override HistoryMetadata GetHistoryMetadata(CancellationToken cancelationToken) => this.vendor.GetHistoryMetadata(cancelationToken);

    public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters) => this.vendor.LoadHistory(requestParameters);
    #endregion History

    #region Orders
    public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token) => this.vendor.GetAllowedOrderTypes(token);

    public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token) => this.vendor.GetPendingOrders(token);
    #endregion Orders

    #region Trading
    public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters parameters) => this.vendor.PlaceOrder(parameters);

    public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters parameters) => this.vendor.ModifyOrder(parameters);

    public override TradingOperationResult CancelOrder(CancelOrderRequestParameters parameters) => this.vendor.CancelOrder(parameters);
    #endregion Trading

    #region Reports
    public override IList<MessageReportType> GetReportsMetaData(CancellationToken token) => this.vendor.GetReportsMetaData(token);

    public override Report GenerateReport(ReportRequestParameters reportRequestParameters) => this.vendor.GenerateReport(reportRequestParameters);
    #endregion Reports
}