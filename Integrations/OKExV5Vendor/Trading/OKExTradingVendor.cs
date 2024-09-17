// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using OKExV5Vendor.API;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.OrderTypes;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket.Models;
using OKExV5Vendor.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace OKExV5Vendor.Trading;

class OKExTradingVendor : OKExMarketVendor, IOKExOrderEntryDataProvider
{
    #region Parameters

    private const string DEFAULT_ACCOUNT_BALANCE_ASSET = "USD";

    private readonly OKExTradingClient client;
    private OKExAccount account;

    private readonly Dictionary<string, OKExBalanceItem> balancesCache;
    private OKExBalance totalBalanceInfo;

    private readonly IDictionary<string, OKExOrder> ordersCache;
    private readonly IDictionary<string, OKExAlgoOrder> algoOrdersCache;
    private readonly IDictionary<string, OKExPosition> positionsCache;

    #endregion Parameters

    public OKExTradingVendor(OKExTradingClient client)
        : base(client)
    {
        this.client = client;

        this.balancesCache = new Dictionary<string, OKExBalanceItem>();
        this.ordersCache = new Dictionary<string, OKExOrder>();
        this.algoOrdersCache = new Dictionary<string, OKExAlgoOrder>();
        this.positionsCache = new Dictionary<string, OKExPosition>();
    }

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
    {
        var result = base.Connect(connectRequestParameters);

        if (result.State != ConnectionState.Connected)
            return result;

        //
        // get account
        //
        this.account = this.client.GetAccount(connectRequestParameters.CancellationToken, out string error);
        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        this.client.SetTradingRateLimitManager(this.account.Id);

        //
        // get orders
        //
        var orders = this.client.GetOrdersList(connectRequestParameters.CancellationToken, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        foreach (var o in orders)
            this.ordersCache[o.OrderId] = o;

        //
        // get algo orders
        //
        var algoOrders = this.client.GetAlgoOrdersList(connectRequestParameters.CancellationToken, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        foreach (var o in algoOrders)
            this.algoOrdersCache[o.AlgoOrderId] = o;

        //
        // get positions
        //
        var positions = this.client.GetPositions(connectRequestParameters.CancellationToken, out error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        foreach (var p in positions)
            this.positionsCache[p.PositionId] = p;

        //
        // get leverages
        //
        if (this.account.IsLeverageSupported)
        {
            var symbolByOkexId = this.allSymbolsCache.Values.Where(s => s.InstrumentType == OKExInstrumentType.Futures || s.InstrumentType == OKExInstrumentType.Swap).ToDictionary(s => s.OKExInstrumentId, s => s);
            var leverageBasedSymbols = symbolByOkexId.Values.ToArray();
            var crossLeverages = this.client.GetLeverages(leverageBasedSymbols, OKExMarginMode.Cross, connectRequestParameters.CancellationToken, out error);

            if (!string.IsNullOrEmpty(error))
                return ConnectionResult.CreateFail(error);

            foreach (var item in crossLeverages)
            {
                if (symbolByOkexId.TryGetValue(item.InstrumentId, out var s))
                    s.CrossLeverage[item.PositionSide.Value] = item.Leverage.Value;
            }

            var isolatedLeverages = this.client.GetLeverages(leverageBasedSymbols, OKExMarginMode.Isolated, connectRequestParameters.CancellationToken, out error);

            if (!string.IsNullOrEmpty(error))
                return ConnectionResult.CreateFail(error);

            foreach (var item in isolatedLeverages)
            {
                if (symbolByOkexId.TryGetValue(item.InstrumentId, out var s))
                    s.IsolatedLeverage[item.PositionSide.Value] = item.Leverage.Value;
            }
        }

        return result;
    }
    public override void Disconnect()
    {
        if (this.client != null)
        {
            this.client.OnBalanceChanged -= this.Client_OnBalanceChanged;
            this.client.OnOrder -= this.Client_OnOrder;
            this.client.OnAlgoOrder -= this.Client_OnAlgoOrder;
            this.client.OnPosition -= this.Client_OnPosition;
        }        

        base.Disconnect();
    }
    public override void OnConnected(CancellationToken token)
    {
        this.client.OnBalanceChanged += this.Client_OnBalanceChanged;
        this.client.OnOrder += this.Client_OnOrder;
        this.client.OnAlgoOrder += this.Client_OnAlgoOrder;
        this.client.OnPosition += this.Client_OnPosition;

        base.OnConnected(token);
    }

    #endregion Connection

    #region Accounts an Rules

    public override IList<MessageRule> GetRules(CancellationToken token)
    {
        var rules = base.GetRules(token);

        foreach (var symbol in this.allSymbolsCache.Values)
        {
            if (symbol.InstrumentType == OKExInstrumentType.Index)
            {
                rules.Add(new MessageRule()
                {
                    Name = Rule.ALLOW_TRADING,
                    SymbolId = symbol.UniqueInstrumentId,
                    Value = false
                });
            }
            else
            {
                //rules.Add(new MessageRule()
                //{
                //    Name = Rule.ALLOW_TRADING,
                //    OrderTypeId = OKExTriggerLimitOrderType.ID,
                //    SymbolId = symbol.UniqueInstrumentId,
                //    Value = symbol.InstrumentType == OKExInstrumentType.Spot
                //});
                //rules.Add(new MessageRule()
                //{
                //    Name = Rule.ALLOW_TRADING,
                //    OrderTypeId = OKExTriggerMarketOrderType.ID,
                //    SymbolId = symbol.UniqueInstrumentId,
                //    Value = symbol.InstrumentType == OKExInstrumentType.Spot
                //});
                //rules.Add(new MessageRule()
                //{
                //    Name = Rule.ALLOW_TRADING,
                //    OrderTypeId = OrderType.Market,
                //    SymbolId = symbol.UniqueInstrumentId,
                //    Value = symbol.InstrumentType != OKExInstrumentType.Option
                //});
                //rules.Add(new MessageRule()
                //{
                //    Name = Rule.ALLOW_TRADING,
                //    OrderTypeId = OrderType.Stop,
                //    SymbolId = symbol.UniqueInstrumentId,
                //    Value = symbol.InstrumentType != OKExInstrumentType.Option
                //});
                //rules.Add(new MessageRule()
                //{
                //    Name = Rule.ALLOW_TRADING,
                //    OrderTypeId = OrderType.StopLimit,
                //    SymbolId = symbol.UniqueInstrumentId,
                //    Value = symbol.InstrumentType != OKExInstrumentType.Option
                //});
            }
        }

        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_TRADING,
            Value = true
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_SL,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_TP,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_MODIFY_TIF,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_MODIFY_ORDER_TYPE,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.LEVEL2_IS_AGGREGATED,
            Value = true
        });
        rules.Add(new MessageRule
        {
            Name = Rule.PLACE_ORDER_TRADING_OPERATION_HAS_ORDER_ID,
            Value = true
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_MODIFY_ORDER,
            OrderTypeId = OrderType.Stop,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_MODIFY_ORDER,
            OrderTypeId = OrderType.StopLimit,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_MODIFY_ORDER,
            OrderTypeId = OKExTriggerLimitOrderType.ID,
            Value = false
        });
        rules.Add(new MessageRule()
        {
            Name = Rule.ALLOW_MODIFY_ORDER,
            OrderTypeId = OKExTriggerMarketOrderType.ID,
            Value = false
        });
        rules.Add(new()
        {
            Name = Rule.ALLOW_REDUCE_ONLY,
            Value = true
        });
        return rules;
    }
    public override IList<MessageAccount> GetAccounts(CancellationToken token)
    {
        return new List<MessageAccount>() { this.CreateCryptoAccountMessage(this.account) };
    }

    #endregion Accounts and Rules

    #region Positions and Orders

    public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token)
    {
        var activeOrders = this.ordersCache.Values.Select(o => this.CreateOpenOrderMessage(o));
        var algoOrders = this.algoOrdersCache.Values.SelectMany(o => this.CreateOpenOrderMessages(o));

        return Enumerable.Concat(activeOrders, algoOrders).ToList();
    }
    public override IList<MessageOpenPosition> GetPositions(CancellationToken token)
    {
        var messages = new List<MessageOpenPosition>();

        foreach (var p in this.positionsCache.Values)
        {
            if (this.TryCreatePositionMessage(p, out var message))
                messages.Add(message);
        }

        return messages;
    }
    public override PnL CalculatePnL(PnLRequestParameters parameters)
    {
        if (!this.positionsCache.TryGetValue(parameters.PositionId, out var position) || !this.allSymbolsCache.TryGetValue(position.UniqueInstrumentId, out var symbol))
            return base.CalculatePnL(parameters);

        var grossPnl = new PnLItem()
        {
            AssetID = position.MarginCurrency,
            Value = position.UnrealizedPnl ?? default,
        };

        if (symbol.ContractType == OKExContractType.Linear)
            grossPnl.ValuePercent = position.UnrealizedPnl / (parameters.Symbol.LotSize * parameters.Quantity * symbol.ContractValue * parameters.OpenPrice) ?? default;
        else if (symbol.ContractType == OKExContractType.Inverse)
            grossPnl.ValuePercent = position.UnrealizedPnl / (parameters.Symbol.LotSize * parameters.Quantity * symbol.ContractValue / parameters.OpenPrice) ?? default;
        else
            grossPnl.ValuePercent = position.UnrealizedPnlRatio.Value;

        return new PnL()
        {
            GrossPnL = grossPnl,
            Swaps = position.Interest.HasValue ? new PnLItem() { Value = position.Interest.Value } : null
        };
    }
    public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token)
    {
        return new List<OrderType>()
        {
            new OKExMarketOrderType(TimeInForce.Default, TimeInForce.IOC) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
            new OKExLimitOrderType(TimeInForce.Default, TimeInForce.FOK, TimeInForce.IOC) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
            new OKExStopMarketOrderType(TimeInForce.Default) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
            new OKExStopLimitOrderType(TimeInForce.Default) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
            new OKExOcoOrderType(TimeInForce.Default) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
            new OKExTriggerMarketOrderType(TimeInForce.Default) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
            new OKExTriggerLimitOrderType(TimeInForce.Default) { BalanceCalculatorFactory = this.CreateOrderEntryBalanceCalculator },
        };
    }

    #endregion Positions and Orders

    #region Trades history

    public override TradesHistoryMetadata GetTradesMetadata() => new TradesHistoryMetadata()
    {
        AllowLocalStorage = true
    };
    public override IList<MessageTrade> GetTrades(TradesHistoryRequestParameters parameters)
    {
        var result = new List<MessageTrade>();

        var from = parameters.From;
        var to = parameters.To;
        var token = parameters.CancellationToken;
        var progress = parameters.Progress;

        var types = new OKExInstrumentType[]
        {
            OKExInstrumentType.Spot,
            OKExInstrumentType.Margin,
            OKExInstrumentType.Swap,
            OKExInstrumentType.Futures,
            OKExInstrumentType.Option
        };

        for (int i = 0; i < types.Length; i++)
        {
            var history = this.client.GetTransactions(types[i], from, to, token, out string error);

            if (token.IsCancellationRequested)
                break;

            if (!string.IsNullOrEmpty(error))
            {
                this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
                break;
            }

            if (history?.Length > 0)
            {
                foreach (var item in history)
                {
                    // можуть бути expired символи. Пропускаю їх.
                    if (!this.allSymbolsCache.ContainsKey(item.UniqueInstrumentId))
                        continue;

                    if (item.HasTradeId)
                        result.Add(this.CreateTradeMessage(item));
                }
            }

            progress?.Report((i + 1) * 100 / types.Length);
        }

        if (result.Count > 0)
            result.Sort((l, r) => l.DateTime.CompareTo(r.DateTime));

        return result;
    }

    #endregion Trades history

    #region Trading opertions

    public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters parameters)
    {
        var result = TradingOperationResult.CreateError(parameters.RequestId, "Incorrect request parameters");

        if (!this.allSymbolsCache.TryGetValue(parameters.SymbolId, out var oKExSymbol))
            return result;

        string error;
        OKExTradingResponce responce;
        var tradeMode = parameters.Symbol.SymbolType == SymbolType.Crypto
            ? OKExTradeMode.Cash
            : OKExTradeMode.Cross;

        if (parameters.AdditionalParameters.TryGetVisibleValue<OKExTradeMode>(OKExOrderTypeHelper.TRADE_MODE_TYPE, out var mode))
            tradeMode = mode;

        if (parameters.OrderTypeId == OrderType.Market || parameters.OrderTypeId == OrderType.Limit)
        {
            var request = new OKExPlaceOrderRequest(oKExSymbol, tradeMode, parameters.Side.ToOKEx(), parameters.ToOKExOrderType());
            this.FillGeneralPlaceRequestProperties(request, parameters, tradeMode, oKExSymbol.InstrumentType);

            request.Tag = OKExConsts.BROKER_ID;
            request.ClientOrderId = parameters.AdditionalParameters.GetVisibleValue<string>(OKExOrderTypeHelper.COMMENT) ?? parameters.Comment;

            if (parameters.OrderTypeId == OrderType.Market && parameters.Symbol.SymbolType == SymbolType.Options)
            {
                var price = parameters.Side == Side.Buy
                    ? parameters.Symbol.Ask
                    : parameters.Symbol.Bid;

                if (!price.IsNanOrDefault())
                    request.Price = price.ToInvariantString();
            }
            else
            {
                if (!double.IsNaN(parameters.Price))
                    request.Price = parameters.Price.ToInvariantString();
            }

            responce = this.client.PlaceOrder(request, parameters.CancellationToken, out error);
        }
        else
        {
            var request = new OKExPlaceAlgoOrderRequest(oKExSymbol, tradeMode, parameters.Side.ToOKEx(), parameters.ToOKExAlgoOrderType(), oKExSymbol.FormattedQuantity(parameters.Quantity));
            this.FillGeneralPlaceRequestProperties(request, parameters, tradeMode, oKExSymbol.InstrumentType);

            if (parameters.OrderTypeId == OrderType.Stop || parameters.OrderTypeId == OrderType.StopLimit)
            {
                if (parameters.Side == Side.Buy && parameters.Symbol.Last < parameters.TriggerPrice || parameters.Side == Side.Sell && parameters.Symbol.Last > parameters.TriggerPrice)
                {
                    request.StopLossTriggerPrice = parameters.TriggerPrice.ToInvariantString();

                    if (!double.IsNaN(parameters.Price))
                        request.StopLossPrice = parameters.Price.ToInvariantString();
                }
                else
                {
                    request.TakeProfitTriggerPrice = parameters.TriggerPrice.ToInvariantString();

                    if (!double.IsNaN(parameters.Price))
                        request.TakeProfitPrice = parameters.Price.ToInvariantString();
                }
            }
            else if (parameters.OrderTypeId == OKExOcoOrderType.ID)
            {
                if (parameters.AdditionalParameters.TryGetVisibleValue(OKExOrderTypeHelper.TAKE_PROFIT_TRIGGER_PRICE, out double tpTriggerPrice) && tpTriggerPrice != default)
                    request.TakeProfitTriggerPrice = tpTriggerPrice.ToInvariantString();

                if (parameters.AdditionalParameters.TryGetVisibleValue(OKExOrderTypeHelper.TAKE_PROFIT_PRICE, out double tpPrice) && tpPrice != default)
                    request.TakeProfitPrice = tpPrice.ToInvariantString();

                if (parameters.AdditionalParameters.TryGetVisibleValue(OKExOrderTypeHelper.STOP_LOSS_TRIGGER_PRICE, out double slTriggerPrice) && slTriggerPrice != default)
                    request.StopLossTriggerPrice = slTriggerPrice.ToInvariantString();

                if (parameters.AdditionalParameters.TryGetVisibleValue(OKExOrderTypeHelper.STOP_LOSS_PRICE, out double slPrice) && slPrice != default)
                    request.StopLossPrice = slPrice.ToInvariantString();
            }
            else if (parameters.OrderTypeId == OKExTriggerLimitOrderType.ID || parameters.OrderTypeId == OKExTriggerMarketOrderType.ID)
            {
                request.TriggerPrice = parameters.TriggerPrice.ToInvariantString();

                if (!double.IsNaN(parameters.Price))
                    request.Price = parameters.Price.ToInvariantString();
            }

            responce = this.client.PlaceAlgoOrder(request, parameters.CancellationToken, out error);
        }

        if (responce != null)
        {
            if (responce.IsSuccess)
                result = TradingOperationResult.CreateSuccess(parameters.RequestId, responce.OrderId);
            else
                result = TradingOperationResult.CreateError(parameters.RequestId, $"[{responce.Code}] {error} {responce.Message}");
        }
        else if (!string.IsNullOrEmpty(error))
        {
            result = TradingOperationResult.CreateError(parameters.RequestId, error);
        }

        return result;
    }
    public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters parameters)
    {
        var result = TradingOperationResult.CreateError(parameters.RequestId, "Incorrect request parameters");

        if (!this.allSymbolsCache.TryGetValue(parameters.SymbolId, out var oKExSymbol))
            return result;

        if (parameters.OrderTypeId == OrderType.Market || parameters.OrderTypeId == OrderType.Limit)
        {
            if (!this.ordersCache.TryGetValue(parameters.OrderId, out var order))
                return result;

            var request = new OKExAmendOrderRequest(oKExSymbol, order.OrderId)
            {
                ClientOrderId = order.ClientOrderId
            };

            // check new size
            if (parameters.Quantity != order.Size)
                request.NewSize = oKExSymbol.FormattedQuantity(parameters.Quantity);

            // check new price
            double newPrice = !double.IsNaN(parameters.TriggerPrice) ? parameters.TriggerPrice : parameters.Price;
            if (newPrice != order.Price)
                request.NewPrice = newPrice.ToInvariantString();

            var responce = this.client.AmendOrder(request, parameters.CancellationToken, out string error);

            if (responce != null)
            {
                if (responce.IsSuccess)
                    result = TradingOperationResult.CreateSuccess(parameters.RequestId, responce.OrderId);
                else
                    result = TradingOperationResult.CreateError(parameters.RequestId, $"[{responce.Code}] {error} {responce.Message}");
            }
        }
        else
            result = TradingOperationResult.CreateError(parameters.RequestId, $"OKEx doesn't allow to modify '{parameters.OrderTypeId}' order.");

        return result;
    }
    public override TradingOperationResult CancelOrder(CancelOrderRequestParameters parameters)
    {
        var result = TradingOperationResult.CreateError(parameters.RequestId, "Incorrect request parameters");

        if (!this.allSymbolsCache.TryGetValue(parameters.Order.Symbol.Id, out var oKExSymbol))
            return result;

        string error;
        OKExTradingResponce responce;

        if (parameters.Order.OrderTypeId == OrderType.Market || parameters.Order.OrderTypeId == OrderType.Limit)
            responce = this.client.CancelOrder(new OKExCancelOrderRequest(oKExSymbol, parameters.OrderId), parameters.CancellationToken, out error);
        else
        {
            string orderId = parameters.Order.GroupId ?? parameters.OrderId;

            if (!this.algoOrdersCache.TryGetValue(orderId, out var algoOrder) || algoOrder.IsCancelling)
                return result;

            // для OCO. На випадок, якщо прийде 2 cancel-реквеста (Відміна SL, а потім TP).
            // перший реквест буде відправлений на сервер (він відмінить обидва ордера), а другий - по флагу "IsCancelling" пропускаємо.
            algoOrder.IsCancelling = true;

            var cancelOrderRequest = new OKExCancelAlgoOrderRequest(oKExSymbol, parameters.Order.GroupId ?? parameters.OrderId);
            responce = this.client.CancelAlgoOrder(cancelOrderRequest, parameters.CancellationToken, out error);
        }

        if (responce != null)
        {
            if (responce.IsSuccess)
                result = TradingOperationResult.CreateSuccess(parameters.RequestId, responce.OrderId);
            else
                result = TradingOperationResult.CreateError(parameters.RequestId, $"[{responce.Code}] {error} {responce.Message}");
        }

        return result;
    }
    public override TradingOperationResult ClosePosition(ClosePositionRequestParameters parameters)
    {
        var result = TradingOperationResult.CreateError(parameters.RequestId, "Incorrect request parameters");

        if (!this.allSymbolsCache.TryGetValue(parameters.Position.Symbol.Id, out var oKExSymbol))
            return result;

        if (!this.positionsCache.TryGetValue(parameters.Position.Id, out var oKExPosition))
            return result;

        if (parameters.Position.Symbol.SymbolType == SymbolType.Options)
        {
            var additionalParameters = new List<SettingItem>();

            OKExOrderTypeHelper.AddOrderBehaviour(additionalParameters, OKExOrderBehaviourType.Close);
            OKExOrderTypeHelper.AddReduceOnly(additionalParameters, true);
            OKExOrderTypeHelper.AddTradeMode(parameters.Position.Symbol, additionalParameters, oKExPosition.MarginMode);

            var placeOrderParameters = new PlaceOrderRequestParameters()
            {
                Account = parameters.Position.Account,
                Symbol = parameters.Position.Symbol,
                CancellationToken = parameters.CancellationToken,
                OrderTypeId = OrderType.Market,
                Quantity = Math.Abs(parameters.CloseQuantity),
                SendingSource = parameters.SendingSource,
                Side = parameters.Position.Side == Side.Buy ? Side.Sell : Side.Buy,
                AdditionalParameters = additionalParameters
            };

            return this.PlaceOrder(new PlaceOrderRequestParameters(placeOrderParameters));
        }
        else
        {
            var request = new OKExClosePositionRequest(oKExSymbol, oKExPosition.MarginMode);

            if (request.MarginMode == OKExTradeMode.Cross)
                request.MarginCurrency = oKExPosition.MarginCurrency;

            if (this.account.PositionMode == OKExPositionMode.LongShort && (parameters.Position.Symbol.SymbolType == SymbolType.Futures || parameters.Position.Symbol.SymbolType == SymbolType.Swap))
                request.PositionSide = oKExPosition.PositionSide;

            var responce = this.client.ClosePosition(request, parameters.CancellationToken, out string error);

            if (!string.IsNullOrEmpty(error))
                result = TradingOperationResult.CreateError(parameters.RequestId, error);
            else if (!string.IsNullOrEmpty(responce?.InstrumentId))
                result = TradingOperationResult.CreateSuccess(parameters.RequestId);
        }

        return result;
    }

    #endregion Trading opertions

    #region Order history

    public override IList<MessageOrderHistory> GetOrdersHistory(OrdersHistoryRequestParameters parameters)
    {
        var result = new List<MessageOrderHistory>();

        var from = parameters.From;
        var to = parameters.To;
        var token = parameters.CancellationToken;
        var progress = parameters.Progress;

        //
        // Active orders
        //
        var types = new OKExInstrumentType[]
        {
            OKExInstrumentType.Spot,
            OKExInstrumentType.Swap,
            OKExInstrumentType.Futures,
            OKExInstrumentType.Option
        };
        for (int i = 0; i < types.Length; i++)
        {
            var orders = this.client.GetHistoryOrders(types[i], from, to, token, out string error);

            if (!string.IsNullOrEmpty(error))
            {
                this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
                break;
            }

            if (token.IsCancellationRequested)
                break;

            if (orders?.Length > 0)
            {
                foreach (var item in orders)
                {
                    // можуть бути expired символи. Пропускаю їх.
                    if (!this.allSymbolsCache.ContainsKey(item.UniqueInstrumentId))
                        continue;

                    result.Add(new MessageOrderHistory(this.CreateOpenOrderMessage(item)));
                }
            }

            progress?.Report((i + 1) * 50 / types.Length);
        }

        //
        // Algo orders
        //
        var algoOrderTypes = new OKExAlgoOrderType[]
        {
            OKExAlgoOrderType.Conditional,
            OKExAlgoOrderType.OCO,
            OKExAlgoOrderType.Trigger
        };
        var algoStates = new OKExAlgoOrderState[]
        {
            OKExAlgoOrderState.Canceled,
            OKExAlgoOrderState.Live,
            OKExAlgoOrderState.OrderFailed,
            OKExAlgoOrderState.Effective
        };

        for (int i = 0; i < algoOrderTypes.Length; i++)
        {
            for (int y = 0; y < algoStates.Length; y++)
            {
                var algoOrders = this.client.GetHistoryAlgoOrders(algoOrderTypes[i], algoStates[y], from, to, token, out string error);

                if (!string.IsNullOrEmpty(error))
                {
                    this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
                    break;
                }

                if (token.IsCancellationRequested)
                    break;

                if (algoOrders?.Length > 0)
                {
                    foreach (var item in algoOrders)
                    {
                        // можуть бути expired символи. Пропускаю їх.
                        if (!this.allSymbolsCache.ContainsKey(item.UniqueInstrumentId))
                            continue;

                        if (this.CreateOpenOrderMessages(item) is IEnumerable<MessageOpenOrder> orders)
                        {
                            foreach (var o in orders)
                                result.Add(new MessageOrderHistory(o));
                        }
                    }
                }
            }

            progress?.Report((i + 1) * 50 / types.Length + 50);
        }

        if (result.Count > 0)
            result.Sort((l, r) => l.LastUpdateTime.CompareTo(r.LastUpdateTime));

        return result;
    }

    #endregion Order history

    #region Reports

    public override IList<MessageReportType> GetReportsMetaData(CancellationToken token)
    {
        return new List<MessageReportType>()
        {
            new MessageReportType()
            {
                Id = OKExConsts.GET_ORDERS_REPORTS_ID,
                Name = loc._("Get orders history"),
                Parameters = new List<SettingItem>
                {
                    new SettingItemSymbol(REPORT_TYPE_PARAMETER_SYMBOL),
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_FROM),
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_TO)
                }
            },
            new MessageReportType()
            {
                Id = OKExConsts.GET_ALGO_ORDERS_REPORTS_ID,
                Name = loc._("Get algo orders history"),
                Parameters = new List<SettingItem>()
                {
                    new SettingItemSymbol(REPORT_TYPE_PARAMETER_SYMBOL),
                    new SettingItemSelectorLocalized(OKExConsts.REPORT_TYPE_PARAMETER_ALGO_ORDER_TYPE, new SelectItem("", (int)OKExAlgoOrderType.Conditional), new List<SelectItem>()
                    {
                        new SelectItem("Conditional", (int)OKExAlgoOrderType.Conditional),
                        new SelectItem("OCO", (int)OKExAlgoOrderType.OCO),
                        new SelectItem("Trigger", (int)OKExAlgoOrderType.Trigger),
                    }),
                    new SettingItemSelectorLocalized(OKExConsts.REPORT_TYPE_PARAMETER_ALGO_ORDER_STATE, new SelectItem("", (int)OKExAlgoOrderState.Effective), new List<SelectItem>()
                    {
                        new SelectItem("Canceled", (int)OKExAlgoOrderState.Canceled),
                        new SelectItem("Effective", (int)OKExAlgoOrderState.Effective),
                        new SelectItem("Live", (int)OKExAlgoOrderState.Live),
                        new SelectItem("Order failed", (int)OKExAlgoOrderState.OrderFailed),
                    }),
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_FROM),
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_TO)
                }
            },
            new MessageReportType()
            {
                Id = OKExConsts.GET_DEPOSIT_REPORTS_ID,
                Name = loc._("Get deposit history"),
                Parameters = new List<SettingItem>()
                {
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_FROM),
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_TO)
                }
            },
            new MessageReportType()
            {
                Id = OKExConsts.GET_WITHDRAWAL_REPORTS_ID,
                Name = loc._("Get withdrawal history"),
                Parameters = new List<SettingItem>()
                {
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_FROM),
                    new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_TO)
                }
            }
        };
    }
    public override Report GenerateReport(ReportRequestParameters reportRequestParameters)
    {
        switch (reportRequestParameters.ReportType.Id)
        {
            case OKExConsts.GET_ORDERS_REPORTS_ID:
                return this.GenerateOrderReport(reportRequestParameters);
            case OKExConsts.GET_ALGO_ORDERS_REPORTS_ID:
                return this.GenerateAlgoOrderReport(reportRequestParameters);
            case OKExConsts.GET_DEPOSIT_REPORTS_ID:
                return this.GenerateDepositReport(reportRequestParameters);
            case OKExConsts.GET_WITHDRAWAL_REPORTS_ID:
                return this.GenerateWithdrawalReport(reportRequestParameters);

            //
            default:
                return null;
        }
    }

    private Report GenerateAlgoOrderReport(ReportRequestParameters parameters)
    {
        var report = new Report();
        report.AddColumn("Symbol Id", ComparingType.String);
        report.AddColumn("Algo order Id", ComparingType.String);
        report.AddColumn("Order Id", ComparingType.String);
        report.AddColumn("Trigger price", ComparingType.Double);
        report.AddColumn("Price", ComparingType.Double);
        report.AddColumn("Size", ComparingType.Double);
        report.AddColumn("Order type", ComparingType.String);
        report.AddColumn("Side", ComparingType.String);
        report.AddColumn("Trade mode", ComparingType.String);
        report.AddColumn("Margin currency", ComparingType.String);
        report.AddColumn("State", ComparingType.String);
        report.AddColumn("Leverage", ComparingType.Double);
        report.AddColumn("Take-profit trigger price", ComparingType.Double);
        report.AddColumn("Take-profit order price", ComparingType.Double);
        report.AddColumn("Stop-loss trigger price", ComparingType.Double);
        report.AddColumn("Stop-loss order price", ComparingType.Double);
        report.AddColumn("Actual order quantity", ComparingType.Double);
        report.AddColumn("Actual order price", ComparingType.Double);
        report.AddColumn("Actual trigger side", ComparingType.String);
        report.AddColumn("Creation time", ComparingType.DateTime);
        report.AddColumn("Trigger time", ComparingType.DateTime);

        DateTime fromDateTime = default;
        DateTime toDateTime = default;
        var algoOrderType = OKExAlgoOrderType.Conditional;
        var algoOrderState = OKExAlgoOrderState.Effective;

        var settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_SYMBOL);
        string symbolId = (settingItem?.Value as Symbol)?.Id;
        if (string.IsNullOrEmpty(symbolId) || !this.allSymbolsCache.TryGetValue(symbolId, out var okexSymbol))
            return report;

        settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_FROM);
        if (settingItem != null)
            fromDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_TO);
        if (settingItem != null)
            toDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        settingItem = parameters.ReportType.Settings.GetItemByName(OKExConsts.REPORT_TYPE_PARAMETER_ALGO_ORDER_TYPE);
        if (settingItem != null)
            algoOrderType = (OKExAlgoOrderType)((SelectItem)settingItem.Value).Value;

        settingItem = parameters.ReportType.Settings.GetItemByName(OKExConsts.REPORT_TYPE_PARAMETER_ALGO_ORDER_STATE);
        if (settingItem != null)
            algoOrderState = (OKExAlgoOrderState)((SelectItem)settingItem.Value).Value;

        if (fromDateTime == default || toDateTime == default)
            return report;

        if (fromDateTime > toDateTime)
            return report;

        var historyOrders = this.client.GetHistoryAlgoOrders(okexSymbol, okexSymbol.InstrumentType, algoOrderType, algoOrderState, fromDateTime, toDateTime, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error ?? "History orders report: Unknown error"));

        foreach (var o in historyOrders)
        {
            var row = new ReportRow();

            row.AddCell(o.OKExInstrumentId);
            row.AddCell(o.AlgoOrderId);
            row.AddCell(o.OrderId);
            row.AddCell(o.TriggerPrice ?? double.NaN, new PriceFormattingDescription(o.TriggerPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.Price ?? double.NaN, new PriceFormattingDescription(o.Price ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.Size ?? double.NaN, new VolumeFormattingDescription(o.Size ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.AlgoOrderType.GetDescription());
            row.AddCell(o.Side.GetDescription());
            row.AddCell(o.TradeMode.GetDescription());
            row.AddCell(o.MarginCurrency);
            row.AddCell(o.State.GetDescription());
            row.AddCell(o.Leverage ?? double.NaN, new PriceFormattingDescription(o.Leverage ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.TakeProfitTriggerPrice ?? double.NaN, new PriceFormattingDescription(o.TakeProfitTriggerPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.TakeProfitPrice ?? double.NaN, new PriceFormattingDescription(o.TakeProfitPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.StopLossTriggerPrice ?? double.NaN, new PriceFormattingDescription(o.StopLossTriggerPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.StopLossPrice ?? double.NaN, new PriceFormattingDescription(o.StopLossPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.ActualSize ?? double.NaN, new VolumeFormattingDescription(o.ActualSize ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.ActualPrice ?? double.NaN, new PriceFormattingDescription(o.ActualPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.ActualSide);
            row.AddCell(o.CreationTime, new DateTimeFormattingDescription(o.CreationTime));
            row.AddCell(o.TriggerTime, new DateTimeFormattingDescription(o.TriggerTime));

            report.Rows.Add(row);

        }

        return report;
    }
    private Report GenerateOrderReport(ReportRequestParameters parameters)
    {
        var report = new Report();
        report.AddColumn("Symbol Id", ComparingType.String);
        report.AddColumn("Symbol type", ComparingType.String);
        report.AddColumn("Order Id", ComparingType.String);
        report.AddColumn("Price", ComparingType.Double);
        report.AddColumn("Size", ComparingType.Double);
        report.AddColumn("Order type", ComparingType.String);
        report.AddColumn("Side", ComparingType.String);
        report.AddColumn("Trade mode", ComparingType.String);
        report.AddColumn("Accumulated fill quantity", ComparingType.Double);
        report.AddColumn("Last filled price", ComparingType.Double);
        report.AddColumn("Last trade Id", ComparingType.String);
        report.AddColumn("Average filled price", ComparingType.Double);
        report.AddColumn("State", ComparingType.String);
        report.AddColumn("Leverage", ComparingType.Double);
        report.AddColumn("Fee currency", ComparingType.String);
        report.AddColumn("Fee", ComparingType.Double);
        report.AddColumn("P&L", ComparingType.Double);
        report.AddColumn("Creation time", ComparingType.DateTime);
        report.AddColumn("Update time", ComparingType.DateTime);

        DateTime fromDateTime = default;
        DateTime toDateTime = default;

        var settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_SYMBOL);
        string symbolId = (settingItem?.Value as Symbol)?.Id;
        if (string.IsNullOrEmpty(symbolId) || !this.allSymbolsCache.TryGetValue(symbolId, out var okexSymbol))
            return report;

        settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_FROM);
        if (settingItem != null)
            fromDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_TO);
        if (settingItem != null)
            toDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        if (fromDateTime == default || toDateTime == default)
            return report;

        if (fromDateTime > toDateTime)
            return report;

        var historyOrders = this.client.GetHistoryOrders(okexSymbol, fromDateTime, toDateTime, null, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error ?? "History orders report: Unknown error"));

        foreach (var o in historyOrders)
        {
            var row = new ReportRow();

            row.AddCell(o.OKExInstrumentId);
            row.AddCell(o.InstrumentType.GetDescription());
            row.AddCell(o.OrderId);
            row.AddCell(o.Price ?? double.NaN, new PriceFormattingDescription(o.Price ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.Size ?? double.NaN, new VolumeFormattingDescription(o.Size ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.OrderType.GetDescription());
            row.AddCell(o.Side.GetDescription());
            row.AddCell(o.TradeMode.GetDescription());
            row.AddCell(o.AccumulatedFillQty ?? double.NaN, new VolumeFormattingDescription(o.Size ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.LastFilledPrice ?? double.NaN, new PriceFormattingDescription(o.LastFilledPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.UniqueLastTradeId);
            row.AddCell(o.AverageFilledPrice ?? double.NaN, new PriceFormattingDescription(o.AverageFilledPrice ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.State.GetDescription());
            row.AddCell(o.Leverage ?? double.NaN, new PriceFormattingDescription(o.Leverage ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.FeeCurrency);
            row.AddCell(o.LastFilledFee ?? o.Fee ?? double.NaN, new PriceFormattingDescription(o.LastFilledFee ?? o.Fee ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.PnL ?? double.NaN, new PriceFormattingDescription(o.PnL ?? double.NaN, o.UniqueInstrumentId));
            row.AddCell(o.CreationTime, new DateTimeFormattingDescription(o.CreationTime));
            row.AddCell(o.UpdateTime, new DateTimeFormattingDescription(o.UpdateTime));

            report.Rows.Add(row);
        }

        return report;
    }
    private Report GenerateDepositReport(ReportRequestParameters parameters)
    {
        var report = new Report();
        report.AddColumn("Hash record", ComparingType.String);
        report.AddColumn("Deposit ID", ComparingType.String);
        report.AddColumn("Currency", ComparingType.String);
        report.AddColumn("Chain", ComparingType.String);
        report.AddColumn("Deposit amount", ComparingType.Double);
        report.AddColumn("State", ComparingType.String);
        report.AddColumn("From", ComparingType.String);
        report.AddColumn("To", ComparingType.String);
        report.AddColumn("Creation time", ComparingType.DateTime);

        DateTime fromDateTime = default;
        DateTime toDateTime = default;

        var settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_FROM);
        if (settingItem != null)
            fromDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_TO);
        if (settingItem != null)
            toDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        if (fromDateTime == default || toDateTime == default)
            return report;

        if (fromDateTime > toDateTime)
            return report;

        var depositRecords = this.client.GetDepositHistory(fromDateTime, toDateTime, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error ?? "History deposit report: Unknown error"));

        foreach (var o in depositRecords)
        {
            var row = new ReportRow();

            row.AddCell(o.HashRecord);
            row.AddCell(o.DepositId);
            row.AddCell(o.Currency);
            row.AddCell(o.Chain);
            row.AddCell(o.DepositAmount ?? double.NaN);
            row.AddCell(o.State.GetDescription());
            row.AddCell(o.From);
            row.AddCell(o.To);
            row.AddCell(o.CreationTime, new DateTimeFormattingDescription(o.CreationTime));

            report.Rows.Add(row);
        }

        return report;
    }
    private Report GenerateWithdrawalReport(ReportRequestParameters parameters)
    {
        var report = new Report();
        report.AddColumn("Hash record", ComparingType.String);
        report.AddColumn("Withdrawal ID", ComparingType.String);
        report.AddColumn("Currency", ComparingType.String);
        report.AddColumn("Chain", ComparingType.String);
        report.AddColumn("Token amount", ComparingType.Double);
        report.AddColumn("Fee", ComparingType.Double);
        report.AddColumn("State", ComparingType.String);
        report.AddColumn("From", ComparingType.String);
        report.AddColumn("To", ComparingType.String);
        report.AddColumn("Creation time", ComparingType.DateTime);

        DateTime fromDateTime = default;
        DateTime toDateTime = default;

        var settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_FROM);
        if (settingItem != null)
            fromDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        settingItem = parameters.ReportType.Settings.GetItemByName(REPORT_TYPE_PARAMETER_DATETIME_TO);
        if (settingItem != null)
            toDateTime = Core.Instance.TimeUtils.ConvertFromSelectedTimeZoneToUTC(((DateTime)settingItem.Value));

        if (fromDateTime == default || toDateTime == default)
            return report;

        if (fromDateTime > toDateTime)
            return report;

        var withdrawalRecords = this.client.GetWithdrawalHistory(fromDateTime, toDateTime, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error ?? "History withdrawal report: Unknown error"));

        foreach (var o in withdrawalRecords)
        {
            var row = new ReportRow();

            row.AddCell(o.HashRecord);
            row.AddCell(o.WithdrawalId);
            row.AddCell(o.Currency);
            row.AddCell(o.Chain);
            row.AddCell(o.TokenAmount ?? double.NaN);
            row.AddCell(o.Fee ?? double.NaN);
            row.AddCell(o.State.GetDescription());
            row.AddCell(o.From);
            row.AddCell(o.To);
            row.AddCell(o.CreationTime, new DateTimeFormattingDescription(o.CreationTime));

            report.Rows.Add(row);
        }

        return report;

    }

    #endregion Reports

    #region Event handlers

    private void Client_OnBalanceChanged(OKExBalance balance)
    {
        if (this.account == null)
            return;

        this.totalBalanceInfo = balance;

        foreach (var item in balance.Details)
        {
            this.balancesCache[item.Currency] = item;

            var message = new MessageCryptoAssetBalances()
            {
                AccountId = this.account.Id,
                AssetId = item.Currency,
                TotalInUSD = item.EquityUSD ?? default,
                Equity = item.Equity ?? default,
                AvailableBalance = item.AvailableBalance ?? item.AvailableEquity ?? default,
                ReservedBalance = item.FrozenBalance ?? default,
            };

            //
            message.TotalBalance = message.AvailableBalance + message.ReservedBalance;

            if (!this.assetsCache.Contains(item.Currency))
                this.PushMessage(this.CreateAssetMessage(item.Currency));

            this.PushMessage(message);
        }

        this.PushMessage(this.CreateCryptoAccountMessage(this.account, balance));
    }

    private void Client_OnPosition(OKExPosition position)
    {
        if (position.IsClosed)
        {
            if (this.positionsCache.ContainsKey(position.PositionId))
            {
                this.PushMessage(new MessageClosePosition() { PositionId = position.PositionId });
                this.positionsCache.Remove(position.PositionId);
            }
        }
        else
        {
            if (!this.positionsCache.TryGetValue(position.PositionId, out var oldPosition) || position.IsModified(oldPosition))
            {
                if (this.TryCreatePositionMessage(position, out var message))
                    this.PushMessage(message);
            }
            this.positionsCache[position.PositionId] = position;
        }
    }
    private void Client_OnAlgoOrder(OKExAlgoOrder algoOrder)
    {
        switch (algoOrder.State.Value)
        {
            case OKExAlgoOrderState.Live:
                {
                    var openMessages = this.CreateOpenOrderMessages(algoOrder);

                    foreach (var m in openMessages)
                    {
                        var history = this.CreateHistoryMessage(m, null, this.algoOrdersCache.ContainsKey(m.OrderId));

                        this.PushMessage(m);
                        this.PushMessage(history);
                    }

                    this.algoOrdersCache[algoOrder.AlgoOrderId] = algoOrder;
                    break;
                }
            case OKExAlgoOrderState.Effective:
            case OKExAlgoOrderState.OrderFailed:
            case OKExAlgoOrderState.Canceled:
                {
                    var openMessages = this.CreateOpenOrderMessages(algoOrder);

                    foreach (var m in openMessages)
                    {
                        this.PushMessage(new MessageCloseOrder() { OrderId = m.OrderId });

                        if (algoOrder.State != OKExAlgoOrderState.Effective)
                        {
                            var history = this.CreateHistoryMessage(m, null);
                            this.PushMessage(history);
                        }
                    }

                    this.algoOrdersCache.Remove(algoOrder.AlgoOrderId);
                    break;
                }
        }
    }
    private void Client_OnOrder(OKExOrder order)
    {
        MessageOrderHistory history = null;
        MessageOpenOrder openMessage = null;
        MessageCloseOrder closeMessage = null;

        switch (order.State)
        {
            case OKExOrderState.Live:
                {
                    openMessage = this.CreateOpenOrderMessage(order);
                    history = this.CreateHistoryMessage(openMessage, order, this.ordersCache.ContainsKey(openMessage.OrderId));
                    this.ordersCache[openMessage.OrderId] = order;
                    break;
                }
            case OKExOrderState.Canceled:
                {
                    closeMessage = new MessageCloseOrder() { OrderId = order.OrderId };
                    history = this.CreateHistoryMessage(this.CreateOpenOrderMessage(order), order);
                    this.ordersCache.Remove(closeMessage.OrderId);
                    break;
                }
            case OKExOrderState.PartiallyFilled:
            case OKExOrderState.Filled:
                {
                    if (order.LastTradeId.HasValue)
                        this.PushMessage(this.CreateTradeMessage(order));

                    history = this.CreateHistoryMessage(this.CreateOpenOrderMessage(order), order);

                    if (order.State == OKExOrderState.Filled)
                    {
                        closeMessage = new MessageCloseOrder() { OrderId = order.OrderId };
                        this.ordersCache.Remove(closeMessage.OrderId);
                    }
                    else
                    {
                        openMessage = this.CreateOpenOrderMessage(order);
                        this.ordersCache[openMessage.OrderId] = order;
                    }
                    break;
                }
        }

        if (openMessage != null)
            this.PushMessage(openMessage);

        if (closeMessage != null)
            this.PushMessage(closeMessage);

        if (history != null)
            this.PushMessage(history);
    }

    private void OnLeverageChangedHandler(SettingItem si, OKExSymbol symbol, OKExPositionSide side, OKExMarginMode mode)
    {
        if (!this.account.IsLeverageSupported)
            return;

        if (si.Value is not double newLeverage)
            return;

        var leverageCache = mode == OKExMarginMode.Cross
            ? symbol.CrossLeverage
            : symbol.IsolatedLeverage;

        if (!leverageCache.TryGetValue(side, out var oldLeverage))
            return;

        if (oldLeverage == newLeverage)
            return;

        var request = new OKExSetLeverageRequest(symbol, newLeverage, mode);

        if (side != OKExPositionSide.Net && mode != OKExMarginMode.Cross)
            request.PosSide = side;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var leverageItem = this.client.SetLeverage(request, cts.Token, out var error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
        else
        {
            if (side != OKExPositionSide.Net && mode == OKExMarginMode.Cross)
            {
                leverageCache[OKExPositionSide.Short] = leverageItem.Leverage.Value;
                leverageCache[OKExPositionSide.Long] = leverageItem.Leverage.Value;
            }
            else
                leverageCache[side] = leverageItem.Leverage.Value;

            this.PushMessage(this.CreateSymbolMessage(symbol, symbol.FundingRate));
            this.PushMessage(MessageDealTicket.CreateInfoDealTicket(si.Name, $"{si.Name} for '{symbol.UniqueInstrumentId}' has beed changed to '{leverageItem.Leverage}'"));
        }
    }

    #endregion Event handlers

    #region Factory methods

    protected override MessageSymbol CreateSymbolMessage(OKExSymbol symbol)
    {
        var message = base.CreateSymbolMessage(symbol);

        var maxLeverage = symbol.MaxLeverage ?? 1d;
        double lever = default;
        if (symbol.CrossLeverage.TryGetValue(OKExPositionSide.Net, out var l))
            lever = l;

        message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
        {
            GroupInfo = OKExConsts.TRADING_INFO_GROUP,
            Id = "cross_net_leverage",
            NameKey = "Cross net leverage",
            ToolTipKey = "Cross net leverage",
            DataType = ComparingType.Double,
            Value = lever,
            SortIndex = 100,
            Visible = lever != default,
            EditingInfo = new EditingInfo()
            {
                SettingItem = new SettingItemDouble("Cross net leverage", lever)
                {
                    Minimum = 1,
                    Maximum = maxLeverage,
                    Increment = 1,
                    DecimalPlaces = 2,
                    UseTradingNumeric = true,
                },
                ValueChanged = (s) => this.OnLeverageChangedHandler(s, symbol, OKExPositionSide.Net, OKExMarginMode.Cross)
            }
        });

        lever = 1;
        if (symbol.CrossLeverage.TryGetValue(OKExPositionSide.Short, out l))
            lever = l;

        message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
        {
            GroupInfo = OKExConsts.TRADING_INFO_GROUP,
            Id = "cross_short_leverage",
            NameKey = "Cross short leverage",
            ToolTipKey = "Cross short leverage",
            DataType = ComparingType.Double,
            Value = lever,
            SortIndex = 100,
            Visible = lever != default,
            EditingInfo = new EditingInfo()
            {
                SettingItem = new SettingItemDouble("Cross short leverage", lever)
                {
                    Minimum = 1,
                    Maximum = maxLeverage,
                    Increment = 1,
                    DecimalPlaces = 2,
                    UseTradingNumeric = true,
                },
                ValueChanged = (s) => this.OnLeverageChangedHandler(s, symbol, OKExPositionSide.Short, OKExMarginMode.Cross)
            }
        });

        lever = 1;
        if (symbol.CrossLeverage.TryGetValue(OKExPositionSide.Long, out l))
            lever = l;

        message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
        {
            GroupInfo = OKExConsts.TRADING_INFO_GROUP,
            Id = "cross_long_leverage",
            NameKey = "Cross long leverage",
            ToolTipKey = "Cross long leverage",
            DataType = ComparingType.Double,
            Value = lever,
            SortIndex = 100,
            Visible = lever != default,
            EditingInfo = new EditingInfo()
            {
                SettingItem = new SettingItemDouble("Cross long leverage", lever)
                {
                    Minimum = 1,
                    Maximum = maxLeverage,
                    Increment = 1,
                    DecimalPlaces = 2,
                    UseTradingNumeric = true,
                },
                ValueChanged = (s) => this.OnLeverageChangedHandler(s, symbol, OKExPositionSide.Long, OKExMarginMode.Cross)
            }
        });

        lever = 1;
        if (symbol.IsolatedLeverage.TryGetValue(OKExPositionSide.Net, out l))
            lever = l;

        message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
        {
            GroupInfo = OKExConsts.TRADING_INFO_GROUP,
            Id = "Isolated_net_leverage",
            NameKey = "Isolated net leverage",
            ToolTipKey = "Isolated net leverage",
            DataType = ComparingType.Double,
            Value = lever,
            SortIndex = 100,
            Visible = lever != default,
            EditingInfo = new EditingInfo()
            {
                SettingItem = new SettingItemDouble("Isolated net leverage", lever)
                {
                    Minimum = 1,
                    Maximum = maxLeverage,
                    Increment = 1,
                    DecimalPlaces = 2,
                    UseTradingNumeric = true,
                },
                ValueChanged = (s) => this.OnLeverageChangedHandler(s, symbol, OKExPositionSide.Net, OKExMarginMode.Isolated)
            }
        });

        lever = 1;
        if (symbol.IsolatedLeverage.TryGetValue(OKExPositionSide.Short, out l))
            lever = l;

        message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
        {
            GroupInfo = OKExConsts.TRADING_INFO_GROUP,
            Id = "Isolated_short_leverage",
            NameKey = "Isolated short leverage",
            ToolTipKey = "Isolated short leverage",
            DataType = ComparingType.Double,
            Value = lever,
            SortIndex = 100,
            Visible = lever != default,
            EditingInfo = new EditingInfo()
            {
                SettingItem = new SettingItemDouble("Isolated short leverage", lever)
                {
                    Minimum = 1,
                    Maximum = maxLeverage,
                    Increment = 1,
                    DecimalPlaces = 2,
                    UseTradingNumeric = true,
                },
                ValueChanged = (s) => this.OnLeverageChangedHandler(s, symbol, OKExPositionSide.Short, OKExMarginMode.Isolated)
            }
        });

        lever = 1;
        if (symbol.IsolatedLeverage.TryGetValue(OKExPositionSide.Long, out l))
            lever = l;

        message.SymbolAdditionalInfo.Add(new AdditionalInfoItem
        {
            GroupInfo = OKExConsts.TRADING_INFO_GROUP,
            Id = "isolated_long_leverage",
            NameKey = "Isolated long leverage",
            ToolTipKey = "Isolated long leverage",
            DataType = ComparingType.Double,
            Value = lever,
            SortIndex = 100,
            Visible = lever != default,
            EditingInfo = new EditingInfo()
            {
                SettingItem = new SettingItemDouble("Isolated long leverage", lever)
                {
                    Minimum = 1,
                    Maximum = maxLeverage,
                    Increment = 1,
                    DecimalPlaces = 2,
                    UseTradingNumeric = true,
                },
                ValueChanged = (s) => this.OnLeverageChangedHandler(s, symbol, OKExPositionSide.Long, OKExMarginMode.Isolated)
            }
        });

        return message;
    }
    private MessageAccount CreateCryptoAccountMessage(OKExAccount cryptoAccount)
    {
        var message = new MessageCryptoAccount()
        {
            AccountName = "Trading account",
            AccountId = cryptoAccount.Id,
            NettingType = cryptoAccount.PositionMode == OKExPositionMode.LongShort ? NettingType.MultiPosition : NettingType.OnePosition,
            AssetId = DEFAULT_ACCOUNT_BALANCE_ASSET,
            AccountAdditionalInfo = new List<AdditionalInfoItem>()
            {
                new AdditionalInfoItem
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    SortIndex = 100,
                    Id = "Is demo account",
                    NameKey = loc.key("Is demo account"),
                    ToolTipKey = loc.key("Is demo account"),
                    DataType = ComparingType.String,
                    Value = this.client.IsDemoMode,
                    Hidden = false
                },
                new AdditionalInfoItem
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    SortIndex = 100,
                    Id = "Account level",
                    NameKey = loc.key("Account level"),
                    ToolTipKey = loc.key("Account level"),
                    DataType = ComparingType.String,
                    Value = cryptoAccount.AccountLevel.GetDescription(),
                    Hidden = false
                },
                new AdditionalInfoItem
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    SortIndex = 110,
                    Id = "Position mode",
                    NameKey = loc.key("Position mode"),
                    ToolTipKey = loc.key("Position mode"),
                    DataType = ComparingType.String,
                    Value = cryptoAccount.PositionMode.GetDescription(),
                    Hidden = false
                },
                new AdditionalInfoItem
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    SortIndex = 110,
                    Id = "Display type of Greeks",
                    NameKey = loc.key("Display type of Greeks"),
                    ToolTipKey = loc.key("Display type of Greeks"),
                    DataType = ComparingType.String,
                    Value = cryptoAccount.GreeksType.GetEnumMember(),
                    Hidden = false
                },
                new AdditionalInfoItem
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    SortIndex = 110,
                    Id = "User level of real trading volume",
                    NameKey = loc.key("User level of the real trading volume"),
                    ToolTipKey = loc.key("User level of the real trading volume"),
                    DataType = ComparingType.String,
                    Value = cryptoAccount.Level,
                    Hidden = false
                },
                new AdditionalInfoItem
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    SortIndex = 110,
                    Id = "Temporary experience user level",
                    NameKey = loc.key("Temporary experience user level"),
                    ToolTipKey = loc.key("Temporary experience user level"),
                    DataType = ComparingType.String,
                    Value = cryptoAccount.LevelTmp,
                    Hidden = false
                }
            }
        };

        return message;
    }
    private MessageAccount CreateCryptoAccountMessage(OKExAccount cryptoAccount, OKExBalance balance)
    {
        var message = this.CreateCryptoAccountMessage(cryptoAccount);

        if (message.AccountAdditionalInfo == null)
            message.AccountAdditionalInfo = new List<AdditionalInfoItem>();

        if (balance.TotalEquity.HasValue)
            message.Balance = balance.TotalEquity.Value;

        if (balance.IsolatedEquity.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "isolated_margin_equity",
                NameKey = loc.key("Isolated margin equity"),
                ToolTipKey = loc.key("Isolated margin equity"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.IsolatedEquity.Value,
                Hidden = false
            });
        }
        if (balance.AdjustedEquity.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "adjusted_effective_equity",
                NameKey = loc.key("Adjusted/Effective equity"),
                ToolTipKey = loc.key("Adjusted/Effective equity"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.AdjustedEquity.Value,
                Hidden = false
            });
        }
        if (balance.PendingOrderFrozenMargin.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "margin_frozen_for_pending_orders",
                NameKey = loc.key("Margin frozen for pending orders"),
                ToolTipKey = loc.key("Margin frozen for pending orders"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.PendingOrderFrozenMargin.Value,
                Hidden = false
            });
        }
        if (balance.InitialMarginRequirement.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "initial_margin_requirement",
                NameKey = loc.key("Initial margin requirement"),
                ToolTipKey = loc.key("Initial margin requirement"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.InitialMarginRequirement.Value,
                Hidden = false
            });
        }
        if (balance.MaintenanceMarginRequirement.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "Maintenance margin requirement",
                NameKey = loc.key("Maintenance margin requirement"),
                ToolTipKey = loc.key("Maintenance margin requirement"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.MaintenanceMarginRequirement.Value,
                Hidden = false
            });
        }
        if (balance.MarginRatio.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "margin_ratio",
                NameKey = loc.key("Margin ratio"),
                ToolTipKey = loc.key("Margin ratio"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.MarginRatio.Value,
                Hidden = false
            });
        }
        if (balance.PositionsQuantity.HasValue)
        {
            message.AccountAdditionalInfo.Add(new AdditionalInfoItem()
            {
                GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                SortIndex = 100,
                Id = "quantity_of_positions",
                NameKey = loc.key("Quantity of positions"),
                ToolTipKey = loc.key("Quantity of positions"),
                DataType = ComparingType.Double,
                CustomAssetID = DEFAULT_ACCOUNT_BALANCE_ASSET,
                Value = balance.PositionsQuantity.Value,
                Hidden = false
            });
        }

        return message;
    }
    private MessageOpenOrder CreateOpenOrderMessage(OKExOrder order)
    {
        var message = new MessageOpenOrder(order.UniqueInstrumentId)
        {
            Price = order.Price ?? default,
            TotalQuantity = order.Size.Value,
            AccountId = this.account.Id,
            Side = order.Side.ToTerminal(),
            OrderId = order.OrderId,
            OrderTypeId = order.ToTerminalOrderType(),
            TimeInForce = order.ToTerminalTIF(),
            LastUpdateTime = order.UpdateTime,
            Status = order.State.ToTerminal(),
            OriginalStatus = order.State.GetEnumMember(),
            FilledQuantity = order.AccumulatedFillQty ?? default,
            AverageFillPrice = order?.AverageFilledPrice ?? default,
        };

        string comment;
        if (order.ClientOrderId != null && order.ClientOrderId.StartsWith(OKExConsts.BROKER_ID))
            comment = order.OrderTag;
        else
            comment = order.ClientOrderId;

        if (!string.IsNullOrEmpty(comment))
            message.Comment = comment;

        if (order.TakeProfitTriggerPrice.HasValue)
            message.TakeProfit = SlTpHolder.CreateTP(order.TakeProfitTriggerPrice.Value);

        if (order.StopLossTriggerPrice.HasValue)
            message.StopLoss = SlTpHolder.CreateTP(order.StopLossTriggerPrice.Value);

        return message;
    }
    private IEnumerable<MessageOpenOrder> CreateOpenOrderMessages(OKExAlgoOrder algoOrder)
    {
        switch (algoOrder.AlgoOrderType)
        {
            case OKExAlgoOrderType.Conditional:
                {
                    var message = new MessageOpenOrder(algoOrder.UniqueInstrumentId)
                    {
                        AccountId = this.account.Id,
                        OrderId = algoOrder.AlgoOrderId,
                        Side = algoOrder.Side.ToTerminal(),
                        Status = algoOrder.State?.ToTerminal() ?? OrderStatus.Opened,
                        TimeInForce = TimeInForce.Default,
                        OriginalStatus = algoOrder.State.GetEnumMember(),
                        TotalQuantity = algoOrder.Size.Value,
                        LastUpdateTime = algoOrder.CreationTime,
                    };

                    if (algoOrder.HasStopLossTriggerPrice)
                        message.TriggerPrice = algoOrder.StopLossTriggerPrice.Value;
                    else if (algoOrder.HasTakeProfitTriggerPrice)
                        message.TriggerPrice = algoOrder.TakeProfitTriggerPrice.Value;

                    if (algoOrder.HasStopLossPrice || algoOrder.HasTakeProfitPrice)
                    {
                        message.OrderTypeId = OrderType.StopLimit;

                        if (algoOrder.HasStopLossPrice)
                            message.Price = algoOrder.StopLossPrice.Value;
                        else if (algoOrder.HasTakeProfitPrice)
                            message.Price = algoOrder.TakeProfitPrice.Value;
                    }
                    else
                        message.OrderTypeId = OrderType.Stop;

                    yield return message;
                    break;
                }
            case OKExAlgoOrderType.OCO:
                {
                    // create TakeProfit order message
                    var tp = new MessageOpenOrder(algoOrder.UniqueInstrumentId)
                    {
                        AccountId = this.account.Id,
                        OrderId = $"{algoOrder.AlgoOrderId}_TP",
                        GroupId = algoOrder.AlgoOrderId,
                        Side = algoOrder.Side.ToTerminal(),
                        Status = algoOrder.State?.ToTerminal() ?? OrderStatus.Opened,
                        TimeInForce = TimeInForce.Default,
                        OriginalStatus = algoOrder.State.GetEnumMember(),
                        TotalQuantity = algoOrder.Size.Value,
                        LastUpdateTime = algoOrder.CreationTime,
                    };

                    tp.TriggerPrice = algoOrder.TakeProfitTriggerPrice ?? double.NaN;

                    if (algoOrder.HasTakeProfitPrice)
                    {
                        tp.OrderTypeId = OrderType.StopLimit;
                        tp.Price = algoOrder.TakeProfitPrice ?? double.NaN;
                    }
                    else
                        tp.OrderTypeId = OrderType.Stop;

                    yield return tp;

                    // create StopLoss order message
                    var sl = new MessageOpenOrder(algoOrder.UniqueInstrumentId)
                    {
                        AccountId = this.account.Id,
                        OrderId = $"{algoOrder.AlgoOrderId}_SL",
                        GroupId = algoOrder.AlgoOrderId,
                        Side = algoOrder.Side.ToTerminal(),
                        Status = algoOrder.State?.ToTerminal() ?? OrderStatus.Opened,
                        TimeInForce = TimeInForce.Default,
                        OriginalStatus = algoOrder.State.GetEnumMember(),
                        TotalQuantity = algoOrder.Size.Value,
                        LastUpdateTime = algoOrder.CreationTime,
                    };

                    sl.TriggerPrice = algoOrder.StopLossTriggerPrice ?? double.NaN;

                    if (algoOrder.HasStopLossPrice)
                    {
                        sl.OrderTypeId = OrderType.StopLimit;
                        sl.Price = algoOrder.StopLossPrice ?? double.NaN;
                    }
                    else
                        sl.OrderTypeId = OrderType.Stop;

                    yield return sl;

                    break;
                }
            case OKExAlgoOrderType.Trigger:
                {
                    var message = new MessageOpenOrder(algoOrder.UniqueInstrumentId)
                    {
                        AccountId = this.account.Id,
                        OrderId = algoOrder.AlgoOrderId,
                        Side = algoOrder.Side.ToTerminal(),
                        Status = algoOrder.State?.ToTerminal() ?? OrderStatus.Opened,
                        TimeInForce = TimeInForce.Default,
                        OriginalStatus = algoOrder.State.GetEnumMember(),
                        TotalQuantity = algoOrder.Size.Value,
                        LastUpdateTime = algoOrder.CreationTime,
                    };

                    message.TriggerPrice = algoOrder.TriggerPrice ?? double.NaN;

                    if (algoOrder.HasPrice)
                    {
                        message.OrderTypeId = OKExTriggerLimitOrderType.ID;
                        message.Price = algoOrder.Price ?? double.NaN;
                    }
                    else
                        message.OrderTypeId = OKExTriggerMarketOrderType.ID;

                    yield return message;
                    break;
                }
        }
    }

    private bool TryCreatePositionMessage(OKExPosition position, out MessageOpenPosition message)
    {
        message = null;
        if (!this.allSymbolsCache.TryGetValue(position.UniqueInstrumentId, out var symbol))
            return false;

        message = new MessageOpenPosition(position.UniqueInstrumentId)
        {
            PositionId = position.PositionId,
            AccountId = this.account.Id,
            LiquidationPrice = position.LiquidationPrice ?? default,
            Side = position.ToTerminalSide(symbol),
            Quantity = Math.Abs(position.Quantity.Value),
            OpenPrice = position.AveragePrice.Value,
            OpenTime = position.CreationTime,
        };

        return true;
    }
    private MessageOrderHistory CreateHistoryMessage(MessageOpenOrder message, OKExOrder order, bool isModified = false)
    {
        return new MessageOrderHistory(message)
        {
            Status = isModified ? OrderStatus.Opened : message.Status,
        };
    }
    private MessageTrade CreateTradeMessage(OKExOrder order)
    {
        var trade = new MessageTrade()
        {
            AccountId = this.account.Id,
            OrderId = order.OrderId,
            Price = order.LastFilledPrice ?? order.AverageFilledPrice ?? default,
            Quantity = order.LastFilledQty ?? order.Size ?? default,
            Side = order.Side.ToTerminal(),
            OrderTypeId = order.ToTerminalOrderType(),
            SymbolId = order.UniqueInstrumentId,
            TradeId = order.UniqueLastTradeId,
            DateTime = order.UpdateTime,
            Comment = order.OrderTag
        };

        if (order.LastFilledFee.HasValue && order.LastFilledFee != 0)
        {
            trade.Fee = new PnLItem()
            {
                Value = order.LastFilledFee.Value,
                AssetID = order.LastFilledFeeCurrency
            };
        }
        if (order.PnL.HasValue && order.PnL != 0)
        {
            string currency = order.Currency;

            if (string.IsNullOrEmpty(currency) && this.allSymbolsCache.TryGetValue(order.UniqueInstrumentId, out var symbol))
            {
                if (symbol.InstrumentType == OKExInstrumentType.Spot || symbol.InstrumentType == OKExInstrumentType.Margin)
                {
                    currency = order.IsTgtEqualToBaseCurrency
                        ? symbol.BaseCurrency
                        : symbol.QuoteCurrency;
                }
                else
                    currency = order.FeeCurrency;
            }

            if (!string.IsNullOrEmpty(currency))
            {
                trade.GrossPnl = new PnLItem()
                {
                    Value = order.PnL.Value,
                    AssetID = currency,
                };
            }
        }

        return trade;
    }
    private MessageTrade CreateTradeMessage(OKExTransaction transaction)
    {
        var message = new MessageTrade()
        {
            AccountId = this.account.Id,
            OrderId = transaction.OrderId,
            TradeId = transaction.UniqueTradeId,
            DateTime = transaction.Time,
            Price = transaction.FillPrice ?? default,
            Side = transaction.Side.ToTerminal(),
            SymbolId = transaction.UniqueInstrumentId,
            Quantity = transaction.FillSize ?? default,
            Fee = new PnLItem()
            {
                AssetID = transaction.FeeCurrency,
                Value = transaction.Fee.Value,
            },
        };

        return message;
    }

    private IBalanceCalculator CreateOrderEntryBalanceCalculator(Symbol symbol, Account account) => new OKExOrderEntryBalanceCalculator(this);

    #endregion Factory methods

    #region IOKExOrderEntryDataProvider

    IReadOnlyDictionary<string, OKExBalanceItem> IOKExOrderEntryDataProvider.Balances => this.balancesCache;
    OKExBalance IOKExOrderEntryDataProvider.TotalInfo => this.totalBalanceInfo;
    OKExAccount IOKExOrderEntryDataProvider.Account => this.account;

    public OKExSymbol GetSymbol(string id)
    {
        if (this.allSymbolsCache.TryGetValue(id, out var symbol))
            return symbol;
        else
            return null;
    }

    #endregion IOKExOrderEntryDataProvider

    #region Misc

    private void FillGeneralPlaceRequestProperties(OKExPlaceOrderBaseRequest request, PlaceOrderRequestParameters parameters, OKExTradeMode tradeMode, OKExInstrumentType symbolType)
    {
        //
        if (tradeMode == OKExTradeMode.Cross)
            request.MarginCurrency = parameters.AdditionalParameters.GetVisibleValue<string>(OKExOrderTypeHelper.MARGIN_CURRENCY);

        //
        if (tradeMode == OKExTradeMode.Cash && symbolType == OKExInstrumentType.Spot)
        {
            switch (parameters.OrderTypeId)
            {
                case OrderType.Market:
                case OrderType.Stop when parameters.Side == Side.Buy:
                case OrderType.StopLimit when parameters.Side == Side.Buy:
                    {
                        if (parameters.QuantityDefinitionSettingName is OrderType.TOTAL)
                        {
                            request.Size = request.Symbol.FormattedTotal(parameters.Total);
                            request.QuantityType = OKExOrderQuantityType.QuoteCurrency;
                        }
                        else
                        {
                            request.Size = request.Symbol.FormattedQuantity(parameters.Quantity);
                            request.QuantityType = OKExOrderQuantityType.BaseCurrency;
                        }
                        break;
                    }

                default:
                    {
                        request.Size = request.Symbol.FormattedQuantity(parameters.Quantity);
                        break;
                    }
            }
        }
        else
        {
            request.Size = request.Symbol.FormattedQuantity(parameters.Quantity);
        }

        //
        if (tradeMode != OKExTradeMode.Cash)
        {
            if (this.account.PositionMode == OKExPositionMode.LongShort && (symbolType == OKExInstrumentType.Futures || symbolType == OKExInstrumentType.Swap))
            {
                request.PositionSide = parameters.AdditionalParameters.GetVisibleValue<OKExOrderBehaviourType>(OKExOrderTypeHelper.ORDER_BEHAVIOUR) == OKExOrderBehaviourType.Open
                    ? request.Side.ToPositionSide()
                    : request.Side.ToPositionSide().Revers();
            }
            else
                request.ReduceOnly = parameters.AdditionalParameters.GetVisibleValue<bool>(OrderType.REDUCE_ONLY);
        }
        else if (symbolType != OKExInstrumentType.Spot && symbolType != OKExInstrumentType.Margin)
        {
            if (this.account.PositionMode == OKExPositionMode.LongShort)
                request.PositionSide = request.Side.ToPositionSide();
        }
    }

    #endregion Misc
}