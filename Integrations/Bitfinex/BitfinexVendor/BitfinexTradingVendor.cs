// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Bitfinex.API;
using Bitfinex.API.Models;
using Bitfinex.API.Models.Requests;
using BitfinexVendor.Extensions;
using BitfinexVendor.Misc;
using BitfinexVendor.OrderTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor;

internal class BitfinexTradingVendor : BitfinexMarketDataVendor
{
    #region Properties

    private static readonly string[] assetsForCrossRates = { "BTC", "USD" };
    private static readonly string[] perpetualAssetsForCrossRates = { "BTCF0", "USTF0" };
    private static readonly IDictionary<string, string> notificationsMap = new Dictionary<string, string>
    {
        [BitfinexNotificationType.WALLET_TRANSFER] = "Transfer"
    };

    private readonly Dictionary<string, MessageCryptoAssetBalances> sentBalancesCache;

    private Task tickersUpdateTask;
    private Task marginInfoUpdateTask;
    private Task symbolsMarginInfoUpdateTask;

    #endregion Properties

    public BitfinexTradingVendor() => this.sentBalancesCache = new Dictionary<string, MessageCryptoAssetBalances>();

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters parameters)
    {
        var baseResult = base.Connect(parameters);

        if (baseResult.State != ConnectionState.Connected)
            return baseResult;

        return ConnectionResult.CreateSuccess();
    }

    private protected override BitfinexApi CreateApiClient(ConnectRequestParameters parameters)
    {
        var settings = parameters.ConnectionSettings;

        string apiKey = settings.GetValue<string>(LOGIN_PARAMETER_GROUP, BitfinexVendor.PARAMETER_API_KEY);
        string secretKey = settings.GetValue<string>(LOGIN_PARAMETER_GROUP, BitfinexVendor.PARAMETER_SECRET_KEY);

        return new BitfinexApi(apiKey, secretKey, parameters.CancellationToken);
    }

    public override void OnConnected(CancellationToken token)
    {
        this.Api.PrivateWebSocketApi.NewData += this.PrivateWebSocketApiOnNewData;
        this.Api.PrivateWebSocketApi.Error += this.WebSocketApiOnError;

        base.OnConnected(token);
    }

    public override void Disconnect()
    {
        if (this.Api?.PrivateWebSocketApi != null)
        {
            this.Api.PrivateWebSocketApi.NewData -= this.PrivateWebSocketApiOnNewData;
            this.Api.PrivateWebSocketApi.Error -= this.WebSocketApiOnError;
        }

        this.sentBalancesCache.Clear();

        base.Disconnect();
    }

    #endregion Connection

    #region Accounts and rules

    public override IList<MessageAccount> GetAccounts(CancellationToken token)
    {
        // Base margin info
        var marginInfo = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetMarginInfo<BitfinexMarginInfo>(BitfinexMarginKey.BASE, token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        if (marginInfo == null)
            throw new InvalidOperationException("Margin info is null");

        this.Context.UpdateMarginInfo(marginInfo);

        // Margin info by symbols
        var symbolsMarginInfo = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetMarginInfo<BitfinexMarginInfo[]>(BitfinexMarginKey.ALL_SYMBOLS, token), token, out error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        if (marginInfo == null)
            throw new InvalidOperationException("Symbols margin info is null");

        this.Context.UpdateSymbolMarginInfo(symbolsMarginInfo);

        // User info
        this.Context.UserInfo = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetUserInfo(token), token, out error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        if (this.Context.UserInfo.IsVerified)
        {
            // Account summary
            this.Context.AccountSummary = this.HandleApiResponse(
                () => this.Api.PrivateRestApiV2.GetAccountSummary(token), token, out error);

            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(error);
        }

        return new List<MessageAccount>
        {
            this.CreateExchangeAccount(),
            this.CreateMarginAccount(this.Context.MarginInfo)
        };
    }

    public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances(CancellationToken token)
    {
        var wallets = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetWallets(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        if (wallets == null)
            throw new InvalidOperationException("Wallets are null");

        this.Context.UpdateWallets(wallets);

        var result = wallets
            .Where(w => w.Type != BitfinexWalletType.FUNDING)
            .Select(this.CreateAssetBalances)
            .ToList();

        foreach (var message in result)
            this.sentBalancesCache[message.AssetId] = message;

        return result;
    }

    public override IList<MessageRule> GetRules(CancellationToken token) => new List<MessageRule>
    {
        new()
        {
            Name = Rule.ALLOW_SL,
            Value = false
        },
        new()
        {
            Name = Rule.ALLOW_TP,
            Value = false
        },
        new MessageRule
        {
            Name = Rule.PLACE_ORDER_TRADING_OPERATION_HAS_ORDER_ID,
            Value = true
        },
        new()
        {
            Name = Rule.ALLOW_REDUCE_ONLY,
            Value = true
        }
    };

    #endregion Accounts and rules

    #region Orders

    public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token)
    {
        IBalanceCalculator CreateBalanceCalculator(Symbol symbol, Account account)
        {
            if (symbol.SymbolType == SymbolType.Crypto)
                return account.Id.Contains(BitfinexWalletType.EXCHANGE) ? new MultiAssetBalanceCalculator() : new BitfinexMarginBalanceCalculator(this.Context);

            return new BitfinexDerivativesBalanceCalculator(this.Context);
        }

        return new List<OrderType>
        {
            new BitfinexLimitOrderType(TimeInForce.Default, TimeInForce.GTT) {BalanceCalculatorFactory = CreateBalanceCalculator},
            new BitfinexMarketOrderType(TimeInForce.Default) {BalanceCalculatorFactory = CreateBalanceCalculator},
            new BitfinexStopOrderType(TimeInForce.Default, TimeInForce.GTT) {BalanceCalculatorFactory = CreateBalanceCalculator},
            new BitfinexStopLimitOrderType(TimeInForce.Default, TimeInForce.GTT) {BalanceCalculatorFactory = CreateBalanceCalculator},
            new BitfinexTrailingStopOrderType(TimeInForce.Default, TimeInForce.GTT) {BalanceCalculatorFactory = CreateBalanceCalculator},
            new BitfinexFillOrKillOrderType(TimeInForce.Default) {BalanceCalculatorFactory = CreateBalanceCalculator},
            new BitfinexImmediateOrCancelOrderType(TimeInForce.Default) {BalanceCalculatorFactory = CreateBalanceCalculator}
        };
    }

    public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token)
    {
        var orders = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetActiveOrders(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        if (orders == null)
            throw new InvalidOperationException("Orders are null");

        return orders.Select(this.CreateOpenOrder).ToList();
    }

    #endregion Orders

    #region Positions

    public override IList<MessageOpenPosition> GetPositions(CancellationToken token)
    {
        var positions = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetPositions(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        if (positions == null)
            throw new InvalidOperationException("Positions are null");

        this.Context.UpdatePositions(positions);

        return positions.Select(this.CreateOpenPosition).ToList();
    }

    public override PnL CalculatePnL(PnLRequestParameters parameters)
    {
        if (string.IsNullOrEmpty(parameters.PositionId) ||
            !long.TryParse(parameters.PositionId, out long positionId) ||
            !this.Context.Positions.TryGetValue(positionId, out var position) ||
            position.PnL == null)
            return base.CalculatePnL(parameters);

        var result = new PnL
        {
            NetPnL = new PnLItem
            {
                AssetID = BitfinexVendor.USER_ASSET_ID,
                Value = position.PnL.Value,
                ValuePercent = position.PnLPercentage / 100 ?? Const.DOUBLE_UNDEFINED
            }
        };

        return result;
    }

    #endregion Positions

    #region User trades

    public override TradesHistoryMetadata GetTradesMetadata() => new() { AllowLocalStorage = true };

    public override IList<MessageTrade> GetTrades(TradesHistoryRequestParameters parameters)
    {
        var from = parameters.From;
        var to = parameters.To;
        var token = parameters.CancellationToken;

        const int LIMIT = 2500;

        BitfinexUserTrade[] trades;
        var stack = new Stack<BitfinexUserTrade>();
        var result = new List<MessageTrade>();

        do
        {
            var request = new BitfinexUserTradesRequest
            {
                Start = new DateTimeOffset(from).ToUnixTimeMilliseconds(),
                End = new DateTimeOffset(to).ToUnixTimeMilliseconds(),
                Limit = LIMIT
            };

            trades = this.HandleApiResponse(
                () => this.Api.PrivateRestApiV2.GetUserTrades(request, token), token, out _, true);
            if (trades == null)
                break;

            foreach (var trade in trades)
                stack.Push(trade);

            var lastTrade = trades.LastOrDefault();
            if (lastTrade == null)
                break;

            to = lastTrade.ExecutionTime.AddMilliseconds(-1);
        }
        while (trades.Length == LIMIT && from < to && !token.IsCancellationRequested);

        while (stack.Any())
        {
            var trade = stack.Pop();
            result.Add(this.CreateTrade(trade));
        }

        return result;
    }

    #endregion User trades

    #region Trading operations

    public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = this.CreateSubmitOrderRequest(parameters);

            return this.Api.PrivateRestApiV2.SubmitOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = this.CreateUpdateOrderRequest(parameters);

            return this.Api.PrivateRestApiV2.UpdateOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    public override TradingOperationResult CancelOrder(CancelOrderRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = new BitfinexCancelOrderRequest
            {
                OrderId = long.Parse(parameters.OrderId, NumberStyles.Integer, CultureInfo.InvariantCulture)
            };

            return this.Api.PrivateRestApiV2.CancelOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    public override TradingOperationResult ClosePosition(ClosePositionRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = this.CreateClosePositionRequest(parameters);

            return this.Api.PrivateRestApiV2.SubmitOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    private TradingOperationResult MakeTradingOperation(Func<Task<BitfinexOrderResponse>> tradingOperation, long requestId, CancellationToken token)
    {
        TradingOperationResult result = default;

        try
        {
            var orderResponse = this.HandleApiResponse(
                tradingOperation.Invoke, token, out string error);

            if (!string.IsNullOrEmpty(error))
                return result = TradingOperationResult.CreateError(requestId, error);

            if (orderResponse == null)
                return result = TradingOperationResult.CreateError(requestId, "Order response is null");

            if (orderResponse.Status != BitfinexStatus.SUCCESS)
                return result = TradingOperationResult.CreateError(requestId, orderResponse.Text);

            return result = TradingOperationResult.CreateSuccess(requestId, orderResponse.Orders.FirstOrDefault()?.Id.ToString());
        }
        catch (Exception ex)
        {
            return result = TradingOperationResult.CreateError(requestId, ex.GetFullMessageRecursive());
        }
    }

    #endregion Trading operations

    #region Orders history

    public override IList<MessageOrderHistory> GetOrdersHistory(OrdersHistoryRequestParameters parameters) =>
        this.LoadOrders(parameters.From, parameters.To, parameters.CancellationToken)
            .Select(this.CreateOrderHistory)
            .ToList();

    #endregion Orders history

    #region Reports

    public override IList<MessageReportType> GetReportsMetaData(CancellationToken token) => new List<MessageReportType>
    {
        new MessageReportType
        {
            Id = BitfinexVendor.REPORT_ORDERS_HISTORY,
            Name = loc.key("Orders history"),
            Parameters = new List<SettingItem>
            {
                new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_FROM),
                new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_TO)
            }
        }
    };

    public override Report GenerateReport(ReportRequestParameters request) => request.ReportType.Id switch
    {
        BitfinexVendor.REPORT_ORDERS_HISTORY => this.GenerateOrdersHistoryReport(request),
        _ => null
    };

    private Report GenerateOrdersHistoryReport(ReportRequestParameters parameters)
    {
        var report = new Report();

        report.AddColumn(loc.key("Order ID"), ComparingType.Long);
        report.AddColumn(loc.key("Group ID"), ComparingType.Long);
        report.AddColumn(loc.key("Client Order ID"), ComparingType.Long);
        report.AddColumn(loc.key("Symbol"), ComparingType.String);
        report.AddColumn(loc.key("Creation time"), ComparingType.DateTime);
        report.AddColumn(loc.key("Update time"), ComparingType.DateTime);
        report.AddColumn(loc.key("Amount"), ComparingType.Double);
        report.AddColumn(loc.key("Orig. amount"), ComparingType.Double);
        report.AddColumn(loc.key("Type"), ComparingType.String);
        report.AddColumn(loc.key("Expiration"), ComparingType.DateTime);
        report.AddColumn(loc.key("Status"), ComparingType.String);
        report.AddColumn(loc.key("Price"), ComparingType.Double);
        report.AddColumn(loc.key("Average price"), ComparingType.Double);
        report.AddColumn(loc.key("Trailing price"), ComparingType.Double);
        report.AddColumn(loc.key("Auxiliary Limit price"), ComparingType.Double);
        report.AddColumn(loc.key("Hidden"), ComparingType.String);

        var settings = parameters.ReportType.Settings;
        var token = parameters.CancellationToken;

        var from = settings.GetValue<DateTime>(REPORT_TYPE_PARAMETER_DATETIME_FROM);
        var to = settings.GetValue<DateTime>(REPORT_TYPE_PARAMETER_DATETIME_TO);

        if (from == default || to == default)
            return report;

        var orders = this.LoadOrders(from, to, token);

        foreach (var order in orders)
        {
            var row = new ReportRow();

            row.AddCell(order.Id);
            row.AddCell(order.GroupId);
            row.AddCell(order.ClientOrderId);
            row.AddCell(order.Symbol);
            row.AddCell(order.CreationTime, new DateTimeFormattingDescription(order.CreationTime));
            row.AddCell(order.UpdateTime, new DateTimeFormattingDescription(order.UpdateTime));
            row.AddCell(order.Amount, new VolumeFormattingDescription(order.Amount, order.Symbol));
            row.AddCell(order.OriginalAmount, new VolumeFormattingDescription(order.OriginalAmount, order.Symbol));
            row.AddCell(order.Type);
            row.AddCell(order.ExpirationTime, order.ExpirationTime != null ? new DateTimeFormattingDescription(order.ExpirationTime.Value) : null);
            row.AddCell(order.Status);
            row.AddCell(order.Price, new PriceFormattingDescription(order.Price, order.Symbol));
            row.AddCell(order.AveragePrice, new PriceFormattingDescription(order.AveragePrice, order.Symbol));
            row.AddCell(order.TrailingPrice, new PriceFormattingDescription(order.TrailingPrice, order.Symbol));
            row.AddCell(order.AuxiliaryLimitPrice, new PriceFormattingDescription(order.AuxiliaryLimitPrice, order.Symbol));
            row.AddCell(order.Hidden.ToString());

            report.Rows.Add(row);
        }

        return report;
    }

    #endregion Reports

    #region Factory

    private MessageAccount CreateExchangeAccount()
    {
        var message = new MessageCryptoAccount
        {
            AccountId = this.ConstructAccountId(BitfinexWalletType.EXCHANGE),
            AccountName = $"{this.Context.UserInfo.Username} - {BitfinexWalletType.EXCHANGE}",
            AccountAdditionalInfo = new List<AdditionalInfoItem>
            {
                new()
                {
                    Id = nameof(Account.Balance),
                    Hidden = true
                },
                new()
                {
                    Id = nameof(Account.AccountCurrency),
                    Hidden = true
                }
            }
        };

        this.FillAccountAdditionalInfo(message);

        return message;
    }

    private MessageAccount CreateMarginAccount(BitfinexMarginInfo marginInfo)
    {
        var message = new MessageCryptoAccount
        {
            AccountId = this.ConstructAccountId(BitfinexWalletType.MARGIN),
            AccountName = $"{this.Context.UserInfo.Username} - {BitfinexWalletType.MARGIN}",
            AssetId = "USD",
            NettingType = NettingType.OnePosition,
            AccountAdditionalInfo = new List<AdditionalInfoItem>
            {
                new()
                {
                    Id = nameof(Account.Balance),
                    Hidden = true
                },
                new()
                {
                    Id = "marginBalance",
                    NameKey = loc.key("Margin balance"),
                    DataType = ComparingType.Double,
                    GroupInfo = BitfinexVendor.ACCOUNT_MARGIN_GROUP,
                    SortIndex = 10,
                    FormatingType = AdditionalInfoItemFormatingType.AssetBalance,
                    Value = marginInfo.MarginBalance.ToDouble()
                },
                new()
                {
                    Id = "netMargin",
                    NameKey = loc.key("Net margin"),
                    DataType = ComparingType.Double,
                    GroupInfo = BitfinexVendor.ACCOUNT_MARGIN_GROUP,
                    SortIndex = 20,
                    FormatingType = AdditionalInfoItemFormatingType.AssetBalance,
                    Value = marginInfo.MarginNet.ToDouble()
                },
                new()
                {
                    Id = "minMargin",
                    NameKey = loc.key("Minimum margin"),
                    DataType = ComparingType.Double,
                    GroupInfo = BitfinexVendor.ACCOUNT_MARGIN_GROUP,
                    SortIndex = 30,
                    FormatingType = AdditionalInfoItemFormatingType.AssetBalance,
                    Value = marginInfo.MarginMin.ToDouble()
                },
                new()
                {
                    Id = "userPnl",
                    NameKey = loc.key("User profit & loss"),
                    DataType = ComparingType.Double,
                    GroupInfo = BitfinexVendor.ACCOUNT_MARGIN_GROUP,
                    SortIndex = 40,
                    FormatingType = AdditionalInfoItemFormatingType.AssetBalance,
                    Value = marginInfo.UserPnL.ToDouble()
                },
                new()
                {
                    Id = "userSwaps",
                    NameKey = loc.key("User swaps"),
                    DataType = ComparingType.Double,
                    GroupInfo = BitfinexVendor.ACCOUNT_MARGIN_GROUP,
                    SortIndex = 50,
                    FormatingType = AdditionalInfoItemFormatingType.AssetBalance,
                    Value = marginInfo.UserSwaps.ToDouble()
                }
            }
        };

        this.FillAccountAdditionalInfo(message);

        return message;
    }

    private void FillAccountAdditionalInfo(MessageAccount message)
    {
        if (this.Context.UserInfo != null)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "email",
                NameKey = loc._("Email"),
                GroupInfo = BitfinexVendor.ACCOUNT_INFO_GROUP,
                Value = this.Context.UserInfo.Email
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "creationTime",
                NameKey = loc._("Creation time"),
                GroupInfo = BitfinexVendor.ACCOUNT_INFO_GROUP,
                FormattingDescription = new DateTimeFormattingDescription(this.Context.UserInfo.CreationTime),
                Value = this.Context.UserInfo.CreationTime
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "isVerified",
                NameKey = loc._("Verified"),
                GroupInfo = BitfinexVendor.ACCOUNT_INFO_GROUP,
                Value = this.Context.UserInfo.IsVerified
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "verificationLevel",
                NameKey = loc._("Verification level"),
                GroupInfo = BitfinexVendor.ACCOUNT_INFO_GROUP,
                Value = this.Context.UserInfo.VerificationLevel switch
                {
                    0 => "Basic",
                    1 => "Basic Plus",
                    2 => "Intermediate",
                    3 => "Full",
                    _ => "Undefined"
                }
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "isMerchantEnabled",
                NameKey = loc._("Merchant enabled"),
                GroupInfo = BitfinexVendor.ACCOUNT_INFO_GROUP,
                Value = this.Context.UserInfo.IsMerchantEnabled
            });
        }

        if (this.Context.AccountSummary != null)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "makerFee",
                NameKey = loc._("Maker fee"),
                GroupInfo = BitfinexVendor.ACCOUNT_FEES_GROUP,
                Value = this.Context.AccountSummary.MakerFee
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "derivativeRebate",
                NameKey = loc._("Derivative rebate"),
                GroupInfo = BitfinexVendor.ACCOUNT_FEES_GROUP,
                Value = this.Context.AccountSummary.DerivativeRebate
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "takerFeeToCrypto",
                NameKey = loc._("Taker fee to crypto"),
                GroupInfo = BitfinexVendor.ACCOUNT_FEES_GROUP,
                Value = this.Context.AccountSummary.TakerFeeToCrypto
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "takerFeeToStable",
                NameKey = loc._("Taker fee to stable"),
                GroupInfo = BitfinexVendor.ACCOUNT_FEES_GROUP,
                Value = this.Context.AccountSummary.TakerFeeToStable
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "takerFeeToFiat",
                NameKey = loc._("Taker fee to fiat"),
                GroupInfo = BitfinexVendor.ACCOUNT_FEES_GROUP,
                Value = this.Context.AccountSummary.TakerFeeToFiat
            });
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem
            {
                Id = "derivativeTakerFee",
                NameKey = loc._("Derivative taker fee"),
                GroupInfo = BitfinexVendor.ACCOUNT_FEES_GROUP,
                Value = this.Context.AccountSummary.DerivativeTakerFee
            });
        }
    }

    private MessageCryptoAssetBalances CreateAssetBalances(BitfinexWallet wallet)
    {
        decimal totalBalance = wallet.Balance;
        string intermediateAsset = default;
        decimal totalIntermediate = 0;
        decimal totalBTC = 0;
        decimal totalUSD = 0;

        bool isPerpetual = wallet.Currency.EndsWith("F0");

        string[] intermediateAssets = isPerpetual ? perpetualAssetsForCrossRates : assetsForCrossRates;
        string btcAsset = isPerpetual ? "BTCF0" : "BTC";
        string usdAsset = isPerpetual ? "USTF0" : "USD";

        foreach (string asset in intermediateAssets)
        {
            if (!this.Context.CrossRates.TryGetCrossRate(wallet.Currency, asset, out double crossRate))
                continue;

            intermediateAsset = asset;
            totalIntermediate = totalBalance * (decimal)crossRate;
            break;
        }

        if (!string.IsNullOrEmpty(intermediateAsset))
        {
            if (this.Context.CrossRates.TryGetCrossRate(intermediateAsset, btcAsset, out double crossRate))
            {
                totalBTC = totalIntermediate * (decimal)crossRate;

                if (this.Context.CrossRates.TryGetCrossRate(btcAsset, usdAsset, out crossRate))
                    totalUSD = totalBTC * (decimal)crossRate;
            }
        }

        return new MessageCryptoAssetBalances
        {
            AccountId = this.ConstructAccountId(wallet.Type),
            AssetId = wallet.Currency,
            TotalBalance = (double)totalBalance,
            AvailableBalance = wallet.AvailableBalance.ToDouble(),
            ReservedBalance = (double)wallet.Balance - wallet.AvailableBalance.ToDouble(),
            TotalInBTC = (double)totalBTC,
            TotalInUSD = (double)totalUSD
        };
    }

    private MessageOpenOrder CreateOpenOrder(BitfinexOrder order)
    {
        var message = new MessageOpenOrder(order.Symbol);

        this.FillOrderMessage(message, order);

        return message;
    }

    private MessageOrderHistory CreateOrderHistory(BitfinexOrder order)
    {
        var message = new MessageOrderHistory(order.Symbol);

        this.FillOrderMessage(message, order);

        return message;
    }

    private void FillOrderMessage(MessageOpenOrder message, BitfinexOrder order)
    {
        message.AccountId = this.ConstructAccountId(order.Type.StartsWith(BitfinexWalletType.EXCHANGE.ToUpperInvariant()) ?
                                                        BitfinexWalletType.EXCHANGE :
                                                        BitfinexWalletType.MARGIN);
        message.OrderId = order.Id.ToString();
        message.GroupId = order.GroupId.ToString();
        message.Comment = order.ClientOrderId.ToString();
        message.LastUpdateTime = order.UpdateTime;
        message.FilledQuantity = Math.Abs(order.OriginalAmount - order.Amount);
        message.TotalQuantity = Math.Abs(order.OriginalAmount);
        message.OrderTypeId = ConvertOrderType(order.Type);
        message.TimeInForce = TimeInForce.Default;
        message.Status = ConvertOrderStatus(order.Status);
        message.Side = order.Amount > 0 ? Side.Buy : Side.Sell;
        message.AverageFillPrice = order.AveragePrice;

        if (order.ExpirationTime != null)
        {
            message.TimeInForce = TimeInForce.GTT;
            message.ExpirationTime = order.ExpirationTime.Value;
        }

        switch (message.OrderTypeId)
        {
            case OrderType.Market:
                message.Price = order.Price;
                break;
            case OrderType.Limit:
                message.Price = order.Price;
                break;
            case OrderType.Stop:
                message.TriggerPrice = order.Price;
                break;
            case OrderType.StopLimit:
                message.Price = order.AuxiliaryLimitPrice;
                message.TriggerPrice = order.Price;
                break;
            case OrderType.TrailingStop:
                message.TriggerPrice = order.Price;
                message.TrailOffset = order.TrailingPrice;
                break;
            case BitfinexVendor.FILL_OR_KILL:
                message.Price = order.Price;
                break;
            case BitfinexVendor.IMMEDIATE_OR_CANCEL:
                message.Price = order.Price;
                break;
        }

        message.AdditionalInfoItems = new List<AdditionalInfoItem>
        {
            new()
            {
                Id = BitfinexVendor.HIDDEN,
                NameKey = "Hidden",
                Value = order.Flags.HasFlag(BitfinexOrderFlags.Hidden)
            },
            new()
            {
                Id = OrderType.REDUCE_ONLY,
                NameKey = "Reduce-only",
                Value = order.Flags.HasFlag(BitfinexOrderFlags.ReduceOnly)
            },
            new()
            {
                Id = OrderType.POST_ONLY,
                NameKey = "Post-only",
                Value = order.Flags.HasFlag(BitfinexOrderFlags.PostOnly) || order.Meta is { IsPostOnly: 1 }
            },
            new()
            {
                Id = BitfinexVendor.LEVERAGE,
                NameKey = "Leverage",
                Hidden = order.Meta?.Leverage == null,
                Value = order.Meta?.Leverage ?? 0
            }
        };
    }

    private static string ConvertOrderType(string orderType) => orderType switch
    {
        BitfinexOrderType.MARKET or BitfinexOrderType.EXCHANGE_MARKET => OrderType.Market,
        BitfinexOrderType.LIMIT or BitfinexOrderType.EXCHANGE_LIMIT => OrderType.Limit,
        BitfinexOrderType.STOP or BitfinexOrderType.EXCHANGE_STOP => OrderType.Stop,
        BitfinexOrderType.STOP_LIMIT or BitfinexOrderType.EXCHANGE_STOP_LIMIT => OrderType.StopLimit,
        BitfinexOrderType.TRAILING_STOP or BitfinexOrderType.EXCHANGE_TRAILING_STOP => OrderType.TrailingStop,
        BitfinexOrderType.FOK or BitfinexOrderType.EXCHANGE_FOK => BitfinexVendor.FILL_OR_KILL,
        BitfinexOrderType.IOC or BitfinexOrderType.EXCHANGE_IOC => BitfinexVendor.IMMEDIATE_OR_CANCEL,
        _ => throw new ArgumentOutOfRangeException(nameof(orderType))
    };

    private static OrderStatus ConvertOrderStatus(string orderStatus)
    {
        if (orderStatus.Contains(BitfinexOrderStatus.ACTIVE))
            return OrderStatus.Opened;

        if (orderStatus.Contains(BitfinexOrderStatus.CANCELED))
            return OrderStatus.Cancelled;

        if (orderStatus.Contains(BitfinexOrderStatus.EXECUTED))
            return OrderStatus.Filled;

        if (orderStatus.Contains(BitfinexOrderStatus.PARTIALLY_FILLED))
            return OrderStatus.PartiallyFilled;

        throw new ArgumentOutOfRangeException(nameof(orderStatus));
    }

    private MessageCloseOrder CreateCloseOrder(BitfinexOrder order) => new MessageCloseOrder
    {
        OrderId = order.Id.ToString()
    };

    private BitfinexSubmitOrderRequest CreateSubmitOrderRequest(OrderRequestParameters parameters)
    {
        bool isMargin = parameters.Account.Id.Contains(BitfinexWalletType.MARGIN);

        var request = new BitfinexSubmitOrderRequest
        {
            Symbol = parameters.SymbolId,
            Type = parameters.OrderTypeId switch
            {
                OrderType.Market => isMargin ? BitfinexOrderType.MARKET : BitfinexOrderType.EXCHANGE_MARKET,
                OrderType.Limit => isMargin ? BitfinexOrderType.LIMIT : BitfinexOrderType.EXCHANGE_LIMIT,
                OrderType.Stop => isMargin ? BitfinexOrderType.STOP : BitfinexOrderType.EXCHANGE_STOP,
                OrderType.StopLimit => isMargin ? BitfinexOrderType.STOP_LIMIT : BitfinexOrderType.EXCHANGE_STOP_LIMIT,
                OrderType.TrailingStop => isMargin ? BitfinexOrderType.TRAILING_STOP : BitfinexOrderType.EXCHANGE_TRAILING_STOP,
                BitfinexVendor.FILL_OR_KILL => isMargin ? BitfinexOrderType.FOK : BitfinexOrderType.EXCHANGE_FOK,
                BitfinexVendor.IMMEDIATE_OR_CANCEL => isMargin ? BitfinexOrderType.IOC : BitfinexOrderType.EXCHANGE_IOC,
                _ => throw new InvalidOperationException("Unsupported order type")
            },
            Amount = (parameters.Side == Side.Buy ? parameters.Quantity : -parameters.Quantity).ToString(CultureInfo.InvariantCulture)
        };

        FillOrderRequest(request, parameters);

        if (parameters.AdditionalParameters.IsOco())
        {
            request.GroupId = new DateTimeOffset(Core.Instance.TimeUtils.DateTimeUtcNow).ToUnixTimeMilliseconds();
            request.PriceOcoStop = parameters.AdditionalParameters.GetOcoStopPrice().FormatPrice();
            request.Flags |= BitfinexOrderFlags.Oco;
        }

        return request;
    }

    private BitfinexUpdateOrderRequest CreateUpdateOrderRequest(ModifyOrderRequestParameters parameters)
    {
        var request = new BitfinexUpdateOrderRequest
        {
            OrderId = long.Parse(parameters.OrderId, NumberStyles.Integer, CultureInfo.InvariantCulture),
            ClientOrderIdDate = $"{Core.Instance.TimeUtils.DateTimeUtcNow:yyyy-MM-dd}",
            Amount = (parameters.Side == Side.Buy ? parameters.Quantity : -parameters.Quantity).ToString(CultureInfo.InvariantCulture)
        };

        FillOrderRequest(request, parameters);

        return request;
    }

    private static void FillOrderRequest(BitfinexOrderRequest request, OrderRequestParameters parameters)
    {
        switch (parameters.OrderTypeId)
        {
            case OrderType.Limit:
                request.Price = parameters.Price.FormatPrice();
                break;
            case OrderType.Stop:
                request.Price = parameters.TriggerPrice.FormatPrice();
                break;
            case OrderType.StopLimit:
                request.AuxiliaryLimitPrice = parameters.Price.FormatPrice();
                request.Price = parameters.TriggerPrice.FormatPrice();
                break;
            case OrderType.TrailingStop:
                request.TrailingPrice = parameters.TrailOffset.FormatPrice();
                break;
            case BitfinexVendor.FILL_OR_KILL:
                request.Price = parameters.Price.FormatPrice();
                break;
            case BitfinexVendor.IMMEDIATE_OR_CANCEL:
                request.Price = parameters.Price.FormatPrice();
                break;
        }

        if (parameters.TimeInForce == TimeInForce.GTT)
            request.TimeInForce = parameters.ExpirationTime;

        if (parameters.AdditionalParameters.IsHidden())
            request.Flags |= BitfinexOrderFlags.Hidden;

        if (parameters.AdditionalParameters.IsReduceOnly())
            request.Flags |= BitfinexOrderFlags.ReduceOnly;

        if (parameters.AdditionalParameters.IsPostOnly())
            request.Flags |= BitfinexOrderFlags.PostOnly;

        request.Leverage = parameters.AdditionalParameters.GetLeverage();

        request.ClientOrderId = int.TryParse(parameters.Comment, out int clientId) ? clientId : parameters.AdditionalParameters.GetClientOrderId();
        request.Meta = new BitfinexMeta
        {
            AffiliateCode = BitfinexVendor.AFFILIATE_CODE
        };
    }

    private MessageOpenPosition CreateOpenPosition(BitfinexPosition position) => new MessageOpenPosition(position.Symbol)
    {
        AccountId = this.ConstructAccountId(BitfinexWalletType.MARGIN),
        Side = position.Amount > 0 ? Side.Buy : Side.Sell,
        PositionId = position.Id.ToString(CultureInfo.InvariantCulture),
        Quantity = Math.Abs(position.Amount),
        OpenPrice = position.BasePrice,
        LiquidationPrice = position.LiquidationPrice ?? default,
        OpenTime = position.CreationTime ?? default
    };

    private MessageClosePosition CreateClosePosition(BitfinexPosition position) => new MessageClosePosition
    {
        PositionId = position.Id.ToString(CultureInfo.InvariantCulture)
    };

    private BitfinexSubmitOrderRequest CreateClosePositionRequest(ClosePositionRequestParameters parameters)
    {
        var request = new BitfinexSubmitOrderRequest
        {
            Symbol = parameters.Position.Symbol.Id,
            Type = BitfinexOrderType.MARKET,
            Amount = ((parameters.Position.Side == Side.Buy ? -1 : 1) * parameters.CloseQuantity).ToString(CultureInfo.InvariantCulture),
            Flags = BitfinexOrderFlags.ReduceOnly,
            Meta = new BitfinexMeta
            {
                AffiliateCode = BitfinexVendor.AFFILIATE_CODE
            }
        };

        if (parameters.Position.Symbol.SymbolType == SymbolType.Swap)
            request.Leverage = BitfinexVendor.MAX_LEVERAGE;

        return request;
    }

    private MessageTrade CreateTrade(BitfinexUserTrade trade)
    {
        var message = new MessageTrade
        {
            TradeId = trade.Id.ToString(CultureInfo.InvariantCulture),
            SymbolId = trade.Pair,
            AccountId = this.ConstructAccountId(!string.IsNullOrEmpty(trade.OrderType) && !trade.OrderType.StartsWith(BitfinexWalletType.EXCHANGE.ToUpperInvariant()) ?
                                                                        BitfinexWalletType.MARGIN :
                                                                        BitfinexWalletType.EXCHANGE),
            Price = trade.Price,
            Quantity = Math.Abs(trade.Amount),
            DateTime = trade.ExecutionTime,
            Side = trade.Amount > 0 ? Side.Buy : Side.Sell,
            OrderId = trade.OrderId.ToString(CultureInfo.InvariantCulture),
            OrderTypeId = !string.IsNullOrEmpty(trade.OrderType) ? ConvertOrderType(trade.OrderType) : string.Empty
        };

        if (trade.Fee != null)
        {
            message.Fee = new PnLItem
            {
                Value = Math.Abs(trade.Fee.Value),
                AssetID = trade.FeeCurrency
            };
        }

        return message;
    }

    #endregion Factory

    #region Misc

    private string ConstructAccountId(string accountType) => $"{accountType}({this.Api.UserId})";

    private IEnumerable<BitfinexOrder> LoadOrders(DateTime from, DateTime to, CancellationToken cancellation)
    {
        const int LIMIT = 2500;

        BitfinexOrder[] orders;
        var stack = new Stack<BitfinexOrder>();

        do
        {
            var request = new BitfinexOrdersHistoryRequest
            {
                Start = from.ToUnixMilliseconds(),
                End = to.ToUnixMilliseconds(),
                Limit = LIMIT
            };

            orders = this.HandleApiResponse(
                () => this.Api.PrivateRestApiV2.GetOrdersHistory(request, cancellation), cancellation, out _, true);
            if (orders == null)
                break;

            foreach (var order in orders)
                stack.Push(order);

            var lastOrder = orders.LastOrDefault();
            if (lastOrder == null)
                break;

            to = lastOrder.CreationTime.AddMilliseconds(-1);
        }
        while (orders.Length == LIMIT && from < to && !cancellation.IsCancellationRequested);

        while (stack.Any())
        {
            var order = stack.Pop();
            yield return order;
        }
    }

    #endregion Misc

    #region Periodic actions

    private void UpdateTickersAction()
    {
        var tickers = this.HandleApiResponse(
            () => this.Api.PublicRestApiV2.GetTickers(this.GlobalCancellation), this.GlobalCancellation, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateTickersAction)}: {error}", LoggingLevel.Error, BitfinexVendor.VENDOR_NAME);
            return;
        }

        this.Context.UpdateTickers(tickers);

        foreach (var message in this.Context.Wallets.Values.Where(w => w.Type != BitfinexWalletType.FUNDING).Select(this.CreateAssetBalances))
        {
            if (this.sentBalancesCache.TryGetValue(message.AssetId, out var sentMessage) && sentMessage.Equals(message))
                continue;

            this.PushMessage(message);
            this.sentBalancesCache[message.AssetId] = message;
        }
    }

    private void UpdateMarginInfoAction()
    {
        var marginInfo = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetMarginInfo<BitfinexMarginInfo>(BitfinexMarginKey.BASE, this.GlobalCancellation), this.GlobalCancellation, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateMarginInfoAction)}: {error}", LoggingLevel.Error, BitfinexVendor.VENDOR_NAME);
            return;
        }

        if (marginInfo == null)
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateMarginInfoAction)}: Margin info is null", LoggingLevel.Error, BitfinexVendor.VENDOR_NAME);
            return;
        }

        this.Context.UpdateMarginInfo(marginInfo);

        var message = this.CreateMarginAccount(this.Context.MarginInfo);
        this.PushMessage(message);
    }

    private void UpdateSymbolsMarginInfoAction()
    {
        var marginInfo = this.HandleApiResponse(
            () => this.Api.PrivateRestApiV2.GetMarginInfo<BitfinexMarginInfo[]>(BitfinexMarginKey.ALL_SYMBOLS, this.GlobalCancellation), this.GlobalCancellation, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateSymbolsMarginInfoAction)}: {error}", LoggingLevel.Error, BitfinexVendor.VENDOR_NAME);
            return;
        }

        if (marginInfo == null)
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateMarginInfoAction)}: Symbols margin info is null", LoggingLevel.Error, BitfinexVendor.VENDOR_NAME);
            return;
        }

        this.Context.UpdateSymbolMarginInfo(marginInfo);
    }

    #endregion Periodic actions

    private void PrivateWebSocketApiOnNewData(object sender, BitfinexEventArgs e)
    {
        if (e.WalletUpdate != null && e.WalletUpdate.Type != BitfinexWalletType.FUNDING)
        {
            this.Context.UpdateWallets(e.WalletUpdate);

            var message = this.CreateAssetBalances(e.WalletUpdate);
            this.PushMessage(message);
        }

        if (e.OrderUpdate != null)
        {
            var orderHistory = this.CreateOrderHistory(e.OrderUpdate);

            if (orderHistory.OrderTypeId != OrderType.TrailingStop)
                this.PushMessage(orderHistory);

            if (orderHistory.Status is OrderStatus.Cancelled or OrderStatus.Filled || orderHistory.FilledQuantity == orderHistory.TotalQuantity)
                this.PushMessage(this.CreateCloseOrder(e.OrderUpdate));
            else
                this.PushMessage(this.CreateOpenOrder(e.OrderUpdate));
        }

        if (e.PositionUpdate != null)
        {
            if (e.Event == BitfinexEvent.POSITION_CLOSE)
                this.PushMessage(this.CreateClosePosition(e.PositionUpdate));
            else
                this.PushMessage(this.CreateOpenPosition(e.PositionUpdate));

            this.Context.UpdatePositions(e.PositionUpdate);
        }

        if (e.UserTrade != null)
            this.PushMessage(this.CreateTrade(e.UserTrade));

        if (e.Notification != null)
        {
            if (!notificationsMap.TryGetValue(e.Notification.Type, out string header))
                header = e.Notification.Type;

            this.PushMessage(MessageDealTicket.CreateInfoDealTicket(header, e.Notification.Text));
        }
    }

    private protected override void OnTimerTick()
    {
        base.OnTimerTick();

        this.tickersUpdateTask ??= Task.Run(this.UpdateTickersAction)
            .ContinueWith(_ => this.tickersUpdateTask = null);

        this.marginInfoUpdateTask ??= Task.Run(this.UpdateMarginInfoAction)
            .ContinueWith(_ => this.marginInfoUpdateTask = null);

        this.symbolsMarginInfoUpdateTask ??= Task.Run(this.UpdateSymbolsMarginInfoAction)
            .ContinueWith(_ => this.symbolsMarginInfoUpdateTask = null);
    }
}