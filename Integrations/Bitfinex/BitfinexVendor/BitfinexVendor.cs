// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace BitfinexVendor;

public class BitfinexVendor : Vendor
{
    #region Consts

    internal const string VENDOR_NAME = "Bitfinex";
    internal const string SYMBOL_SEPARATOR = ":";
    internal const int EXCHANGE_ID = 1;
    internal const string TRADING_INFO_GROUP = "#20.Trading info";
    internal const string FUNDING_GROUP = "#30.Funding";
    internal const string ACCOUNT_MARGIN_GROUP = "#20.Margin";
    internal const string ACCOUNT_FEES_GROUP = "#70.Fees";
    internal const string ACCOUNT_INFO_GROUP = "#80.Info";

    private const string CONNECTION_INFO = "Info";
    private const string CONNECTION_TRADING = "Trading";

    internal const string PARAMETER_API_KEY = "apiKey";
    internal const string PARAMETER_SECRET_KEY = "secretKey";

    internal const string USER_ASSET_ID = "USD";

    internal const string HIDDEN = "hidden";
    internal const string OCO = "oco";
    internal const string OCO_STOP_PRICE = "ocoStopPrice";
    internal const string LEVERAGE = "leverage";
    internal const string ALLOW_MARGIN = "allowMargin";
    internal const string CLIENT_ORDER_ID = "clientOrderId";

    internal const string FILL_OR_KILL = "Fill or kill";
    internal const string IMMEDIATE_OR_CANCEL = "Immediate or Cancel";

    internal const int REPORT_ORDERS_HISTORY = 0;
    internal const int MAX_LEVERAGE = 100;

    internal const string AFFILIATE_CODE = "bNg0x_VTz";

    #endregion Consts

    #region Integration details

    public static VendorMetaData GetVendorMetaData() => new()
    {
        VendorName = VENDOR_NAME,
        VendorDescription = loc.key("Market data connection. Trading coming soon."),
        GetDefaultConnections = () => new List<ConnectionInfo>
        {
            CreateDefaultConnectionInfo("Bitfinex", VENDOR_NAME, "BitfinexVendor\\Bitfinex.svg",links:new List<ConnectionInfoLink>()
                {
                    new ConnectionInfoLink()
                    {
                        Text = "Register account",
                        URL = @"https://bitfinex.com/?refcode=bNg0x_VTz"
                    }
                })
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
                    new SettingItemPassword(PARAMETER_API_KEY, new PasswordHolder())
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
        }
    };

    #endregion Integration details

    #region Properties

    private Vendor currentVendor;

    #endregion Properties

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return ConnectionResult.CreateFail(loc._("Network does not available"));

        var settingItem = connectRequestParameters.ConnectionSettings.GetItemByPath(LOGIN_PARAMETER_GROUP, CONNECTION);
        if (settingItem is not { Value: SelectItem selectItem })
            return ConnectionResult.CreateFail("Can't find connection parameters");

        this.currentVendor = selectItem.Value.ToString() == CONNECTION_INFO ?
            new BitfinexMarketDataVendor() :
            new BitfinexTradingVendor();

        this.currentVendor.NewMessage += this.CurrentVendorOnNewMessage;

        return this.currentVendor.Connect(connectRequestParameters);
    }

    public override void OnConnected(CancellationToken token) => this.currentVendor.OnConnected(token);

    public override void Disconnect() => this.currentVendor.Disconnect();

    public override PingResult Ping() => this.currentVendor.Ping();

    #endregion

    #region Accounts and rules

    public override IList<MessageAccount> GetAccounts(CancellationToken token) => this.currentVendor.GetAccounts(token);

    public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances(CancellationToken token) => this.currentVendor.GetCryptoAssetBalances(token);

    public override IList<MessageRule> GetRules(CancellationToken token) => this.currentVendor.GetRules(token);

    public override IList<MessageAccountOperation> GetAccountOperations(CancellationToken token) => this.currentVendor.GetAccountOperations(token);

    #endregion Accounts and rules

    #region Symbols and symbol groups

    public override IList<MessageSymbol> GetSymbols(CancellationToken token) => this.currentVendor.GetSymbols(token);

    public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => this.currentVendor.GetSymbolTypes(token);

    public override IList<MessageAsset> GetAssets(CancellationToken token) => this.currentVendor.GetAssets(token);

    public override IList<MessageExchange> GetExchanges(CancellationToken token) => this.currentVendor.GetExchanges(token);

    #endregion

    #region Subscriptions

    public override void SubscribeSymbol(SubscribeQuotesParameters parameters) => this.currentVendor.SubscribeSymbol(parameters);

    public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters) => this.currentVendor.UnSubscribeSymbol(parameters);

    #endregion Subscriptions

    #region Orders and positions

    public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token) => this.currentVendor.GetAllowedOrderTypes(token);

    public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token) => this.currentVendor.GetPendingOrders(token);

    public override IList<MessageOpenPosition> GetPositions(CancellationToken token) => this.currentVendor.GetPositions(token);

    public override PnL CalculatePnL(PnLRequestParameters parameters) => this.currentVendor.CalculatePnL(parameters);

    #endregion Orders and positions

    #region Trading operations: placing, modifying, cancelling orders

    public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters request) => this.currentVendor.PlaceOrder(request);

    public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters request) => this.currentVendor.ModifyOrder(request);

    public override TradingOperationResult CancelOrder(CancelOrderRequestParameters request) => this.currentVendor.CancelOrder(request);

    public override TradingOperationResult ClosePosition(ClosePositionRequestParameters parameters) => this.currentVendor.ClosePosition(parameters);

    public override MarginInfo GetMarginInfo(OrderRequestParameters orderRequestParameters) => this.currentVendor.GetMarginInfo(orderRequestParameters);

    #endregion Trading operations: placing, modifying, cancelling orders

    #region History

    public override HistoryMetadata GetHistoryMetadata(CancellationToken cancellation) => this.currentVendor.GetHistoryMetadata(cancellation);

    public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters) => this.currentVendor.LoadHistory(requestParameters);

    #endregion History

    #region Volume analysis

    public override VolumeAnalysisMetadata GetVolumeAnalysisMetadata() => this.currentVendor.GetVolumeAnalysisMetadata();

    public override VendorVolumeAnalysisByPeriodResponse LoadVolumeAnalysis(VolumeAnalysisByPeriodRequestParameters requestParameters)
        => this.currentVendor.LoadVolumeAnalysis(requestParameters);

    #endregion Volume analysis

    #region Trades history

    public override TradesHistoryMetadata GetTradesMetadata() => this.currentVendor.GetTradesMetadata();

    public override IList<MessageTrade> GetTrades(TradesHistoryRequestParameters parameters) => this.currentVendor.GetTrades(parameters);

    #endregion Trades history

    #region Orders history

    public override IList<MessageOrderHistory> GetOrdersHistory(OrdersHistoryRequestParameters parameters) => this.currentVendor.GetOrdersHistory(parameters);

    #endregion Orders history

    #region Reports

    public override IList<MessageReportType> GetReportsMetaData(CancellationToken token) => this.currentVendor.GetReportsMetaData(token);

    public override Report GenerateReport(ReportRequestParameters reportRequestParameters) => this.currentVendor.GenerateReport(reportRequestParameters);

    #endregion Reports

    private void CurrentVendorOnNewMessage(object sender, VendorEventArgs e) => this.PushMessage(e.Message);
}