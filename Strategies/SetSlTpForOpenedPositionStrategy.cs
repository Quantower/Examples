// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace Strategies;

public class SetSlTpForOpenedPositionStrategy : Strategy, ICurrentSymbol, ICurrentAccount
{
    [InputParameter("Symbol")]
    public Symbol CurrentSymbol { get; set; }

    [InputParameter("Account")]
    public Account CurrentAccount { get; set; }

    [InputParameter]
    public double Quantity { get; set; }

    public override string[] MonitoringConnectionsIds => new[] { this.CurrentSymbol?.ConnectionId };

    private TradingOperationResult positionOpenResult;
    private Position currentPosition;

    public SetSlTpForOpenedPositionStrategy()
    {
        this.Name = nameof(SetSlTpForOpenedPositionStrategy);
        this.Description = "This strategy will open position and than place SL and TP for it";
    }

    protected override void OnRun()
    {
        if (this.CurrentSymbol == null || this.CurrentAccount == null || this.CurrentSymbol.ConnectionId != this.CurrentAccount.ConnectionId)
        {
            this.LogError("Incorrect input parameters... Symbol or Account are not specified or they have different connection id.");
            return;
        }

        this.CurrentSymbol = Core.GetSymbol(this.CurrentSymbol.CreateInfo());
        this.CurrentAccount = Core.GetAccount(this.CurrentAccount.CreateInfo());

        this.CurrentSymbol.NewLast += this.CurrentSymbolOnNewLast;
        Core.PositionAdded += this.CoreOnPositionAdded;
    }

    protected override void OnStop()
    {
        if (this.CurrentSymbol != null)
            this.CurrentSymbol.NewLast -= this.CurrentSymbolOnNewLast;

        Core.PositionAdded -= this.CoreOnPositionAdded;
    }

    private void CurrentSymbolOnNewLast(Symbol symbol, Last last)
    {
        if (this.positionOpenResult != null)
            return;

        // try to open position
        this.positionOpenResult = Core.PlaceOrder(this.CurrentSymbol, this.CurrentAccount, Side.Buy, TimeInForce.Default, this.Quantity);

        if (this.positionOpenResult.Status == TradingOperationResultStatus.Failure)
            this.LogError(this.positionOpenResult.Message);
    }

    private void CoreOnPositionAdded(Position position)
    {
        if (this.currentPosition != null)
            return;

        this.currentPosition = position;

        this.PlaceCloseOrder(this.currentPosition, CloseOrderType.StopLoss, 0.0025);
        this.PlaceCloseOrder(this.currentPosition, CloseOrderType.TakeProfit, 0.005);
    }

    private void PlaceCloseOrder(Position position, CloseOrderType closeOrderType, double priceOffset)
    {
        var request = new PlaceOrderRequestParameters
        {
            Symbol = this.CurrentSymbol,
            Account = this.CurrentAccount,
            Side = position.Side == Side.Buy ? Side.Sell : Side.Buy,
            Quantity = position.Quantity,
            PositionId = position.Id,
            AdditionalParameters = new List<SettingItem>
            {
                new SettingItemBoolean(OrderType.REDUCE_ONLY, true)
            }
        };

        if (closeOrderType == CloseOrderType.StopLoss)
        {
            var orderType = this.CurrentSymbol.GetAlowedOrderTypes(OrderTypeUsage.CloseOrder).FirstOrDefault(ot => ot.Behavior == OrderTypeBehavior.Stop);

            if (orderType == null)
            {
                this.LogError("Can't find order type for SL");
                return;
            }

            request.OrderTypeId = orderType.Id;
            request.TriggerPrice = position.Side == Side.Buy ? position.OpenPrice - priceOffset : position.OpenPrice + priceOffset;
        }
        else
        {
            var orderType = this.CurrentSymbol.GetAlowedOrderTypes(OrderTypeUsage.CloseOrder).FirstOrDefault(ot => ot.Behavior == OrderTypeBehavior.Limit);

            if (orderType == null)
            {
                this.LogError("Can't find order type for TP");
                return;
            }

            request.OrderTypeId = orderType.Id;
            request.Price = position.Side == Side.Buy ? position.OpenPrice + priceOffset : position.OpenPrice - priceOffset;
        }

        var result = Core.PlaceOrder(request);

        if (result.Status == TradingOperationResultStatus.Failure)
            this.LogError(result.Message);
    }
}