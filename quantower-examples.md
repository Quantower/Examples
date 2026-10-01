# Quantower API Examples - Comprehensive Guide

**Last Updated:** 2025-11-12
**Repository:** https://github.com/Quantower/Examples
**Official API Docs:** https://api.quantower.com/

---

## Table of Contents

1. [Overview](#overview)
2. [Repository Structure](#repository-structure)
3. [Core API Concepts](#core-api-concepts)
4. [Market Data & Symbols](#market-data--symbols)
5. [Historical Data](#historical-data)
6. [Order Management](#order-management)
7. [Indicators](#indicators)
8. [Strategies](#strategies)
9. [Advanced Features](#advanced-features)
10. [Code Reference Index](#code-reference-index)

---

## Overview

This repository contains examples demonstrating the Quantower trading platform API. Quantower is a professional trading platform that supports multiple asset classes and connections to various exchanges.

**Key Technologies:**
- Language: C#
- Platform: .NET
- Integration: Visual Studio Extension available
- Architecture: Event-driven, real-time data processing

**What You Can Build:**
- Custom technical indicators
- Automated trading strategies
- Order placement strategies
- Custom UI plugins
- Exchange integrations
- Webhook-based trading systems

---

## Repository Structure

```
/franco-1t/
├── Common/                     # Basic API usage examples
│   ├── PlaceOrderExamples.cs
│   ├── HistoryExamples.cs
│   ├── SubscribeMarketDataExamples.cs
│   ├── PlaceOrderWithMultipleSlTP.cs
│   ├── Webhook_Strategy_Example.cs
│   ├── PowerTradesExamples.cs
│   ├── VwapExamples.cs
│   ├── SearchSymbolsExample.cs
│   ├── LinkOrdersAsOCOExample.cs
│   ├── AccessOrderTypeAdvancedParameters.cs
│   └── SettingsRelationsExample.cs
│
├── IndicatorExamples/          # Custom technical indicators
│   ├── SimpleMovingAverage.cs
│   ├── ExponentialMovingAverage.cs
│   ├── DoubleSMAIndicator.cs
│   ├── AccessIndicatorByName.cs
│   ├── Level2WithMBOIndicator.cs
│   ├── DrawOnBars.cs
│   ├── AsynchronousIndicator.cs
│   └── [15+ more indicator examples]
│
├── Strategies/                 # Trading strategy examples
│   ├── SimpleMACross.cs
│   └── SetSlTpForOpenedPositionStrategy.cs
│
├── PlaceOrderStrategies/       # Custom order execution strategies
│   ├── MarketIfTouchedOrderPlacingStrategy/
│   │   ├── MarketIfTouchedOrderPlacingStrategy.cs
│   │   └── PlaceOrderIfTouchedOrderPlacingStrategy.cs
│   └── RepeatOrderPlacing/
│       └── RepeatOrderPlacing.cs
│
├── Plugins/                    # UI plugin examples
│   ├── BrowserBasedPlugin/
│   └── GDIBasedPlugin/
│
└── Integrations/               # Exchange integrations
    ├── OKExV5Vendor/
    └── Gateio/
```

---

## Core API Concepts

### The `Core.Instance` Pattern

**Everything starts with `Core.Instance`** - the central singleton providing access to all platform functionality:

```csharp
using TradingPlatform.BusinessLayer;

// Access symbols
Symbol[] symbols = Core.Instance.Symbols;

// Access accounts
Account[] accounts = Core.Instance.Accounts;

// Access positions
Position[] positions = Core.Instance.Positions;

// Place orders
TradingOperationResult result = Core.Instance.PlaceOrder(request);

// Access indicators
Indicator sma = Core.Instance.Indicators.BuiltIn.SMA(20, PriceType.Close);

// Logging
Core.Instance.Loggers.Log("Message", LoggingLevel.System);
```

### Event-Driven Architecture

The API uses events extensively for real-time updates:

```csharp
// Position events
Core.PositionAdded += OnPositionAdded;
Core.PositionRemoved += OnPositionRemoved;

// Order events
Core.OrdersHistoryAdded += OnOrderHistoryAdded;

// Trade events
Core.TradeAdded += OnTradeAdded;

// Market data events (on Symbol object)
symbol.NewQuote += OnNewQuote;
symbol.NewLast += OnNewLast;
symbol.NewLevel2 += OnNewLevel2;
```

**Important:** Always unsubscribe from events in cleanup methods to prevent memory leaks.

---

## Market Data & Symbols

### Finding Symbols

**File Reference:** `Common/SearchSymbolsExample.cs`

```csharp
// Get first available symbol
Symbol symbol = Core.Instance.Symbols.FirstOrDefault();

// Find by name
symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "EUR/USD");

// Search for symbols
var searchResult = Core.Instance.SearchSymbols(new SearchSymbolsRequestParameters {
    FilterName = "BTC",
    SymbolTypes = new[] { SymbolType.Futures },
    ConnectionId = connection.Id
});

// Get full symbol information (required after search)
Symbol fullSymbol = Core.Instance.GetSymbol(new GetSymbolRequestParameters {
    SymbolId = searchResult.First().Id
});
```

### Real-Time Market Data

**File Reference:** `Common/SubscribeMarketDataExamples.cs:20-97`

```csharp
// Subscribe to quotes (bid/ask)
symbol.NewQuote += (symbol, quote) => {
    double bid = quote.Bid;
    double bidSize = quote.BidSize;
    double ask = quote.Ask;
    double askSize = quote.AskSize;
};

// Subscribe to last trades
symbol.NewLast += (symbol, last) => {
    double price = last.Price;
    double size = last.Size;
};

// Subscribe to Level2 (depth of market)
symbol.NewLevel2 += (symbol, level2, dom) => {
    // Snapshot update
    if (dom != null) {
        List<Level2Quote> bids = dom.Bids;
        List<Level2Quote> asks = dom.Asks;
    }

    // Incremental update
    if (level2 != null) {
        QuotePriceType priceType = level2.PriceType; // Bid or Ask
        double price = level2.Price;
        double size = level2.Size;
        bool isClosed = level2.Closed; // Level should be removed
    }
};

// Subscribe to daily OHLC updates
symbol.NewDayBar += (symbol, dayBar) => {
    double open = dayBar.Open;
    double high = dayBar.High;
    double low = dayBar.Low;
    double prevClose = dayBar.PreviousClose;
};
```

### Accessing Current Symbol Data

```csharp
// Current market prices (no subscription needed)
double bid = symbol.Bid;
double ask = symbol.Ask;
double last = symbol.Last;
double open = symbol.Open;
double high = symbol.High;
double low = symbol.Low;

// Symbol properties
string name = symbol.Name;
string connectionId = symbol.ConnectionId;
SymbolType type = symbol.SymbolType;
double minQuantity = symbol.MinQuantity;
double maxQuantity = symbol.MaxQuantity;
```

---

## Historical Data

**File Reference:** `Common/HistoryExamples.cs`

### Loading History

```csharp
// Simple time-based history (1-minute bars for 1 day)
HistoricalData history = symbol.GetHistory(Period.MIN1, DateTime.Now.AddDays(-1));

// Tick data (bid/ask ticks for 1 hour)
history = symbol.GetHistory(Period.TICK1, HistoryType.BidAsk, DateTime.Now.AddHours(-1));

// With custom aggregations (Renko)
history = symbol.GetHistory(
    new HistoryAggregationRenko(
        Period.MIN1,
        symbol.HistoryType,
        10,
        RenkoStyle.AdvancedClassic
    ),
    DateTime.Now.AddDays(-1)
);

// Advanced request with all parameters
history = symbol.GetHistory(new HistoryRequestParameters {
    Aggregation = new HistoryAggregationPointsAndFigures(
        Period.TICK1,
        symbol.HistoryType,
        100,
        50,
        PointsAndFiguresStyle.HighLow
    ),
    FromTime = DateTime.Now.AddDays(-2),
    ToTime = DateTime.Now.AddDays(-1),  // If specified, no real-time updates
    ForceReload = true  // Bypass cache, load from server
});
```

### Accessing Historical Data

**Important:** Historical data uses **right-to-left indexing** (most recent = index 0):

```csharp
// Get current (most recent) bar
IHistoryItem current = history[0];

// Get previous bar
IHistoryItem previous = history[1];

// Get bar at offset 10
IHistoryItem older = history[10];

// Alternative: left-to-right indexing
IHistoryItem third = history[3, SeekOriginHistory.Begin];

// Total count
int count = history.Count;
```

### Reading History Item Data

**File Reference:** `Common/HistoryExamples.cs:66-118`

```csharp
// Generic access (works for all types)
DateTime leftTime = historyItem.TimeLeft;
double open = historyItem[PriceType.Open];      // Bar history only
double high = historyItem[PriceType.High];      // Bar history only
double low = historyItem[PriceType.Low];        // Bar history only
double close = historyItem[PriceType.Close];    // Bar history only
double volume = historyItem[PriceType.Volume];  // Bar and Last ticks
double ticks = historyItem[PriceType.Ticks];    // Bar history only

// Type-specific access (better performance)
if (historyItem is HistoryItemBar bar) {
    double open = bar.Open;
    double high = bar.High;
    double low = bar.Low;
    double close = bar.Close;
    double median = bar.Median;     // (High + Low) / 2
    double typical = bar.Typical;   // (High + Low + Close) / 3
    double weighted = bar.Weighted; // (High + Low + Close + Close) / 4
}

if (historyItem is HistoryItemTick tick) {
    double bid = tick.Bid;
    double bidSize = tick.BidSize;
    double ask = tick.Ask;
    double askSize = tick.AskSize;
}

if (historyItem is HistoryItemLast lastTick) {
    double price = lastTick.Price;
    double volume = lastTick.Volume;
}
```

### Real-Time History Updates

**File Reference:** `Common/HistoryExamples.cs:40-44`

```csharp
// Subscribe to bar updates (current bar being formed)
history.HistoryItemUpdated += (sender, e) => {
    IHistoryItem updated = e.HistoryItem;
    // Process updated bar
};

// Subscribe to new bars
history.NewHistoryItem += (sender, e) => {
    IHistoryItem newBar = e.HistoryItem;
    // Process new completed bar
};
```

---

## Order Management

### Basic Order Placement

**File Reference:** `Common/PlaceOrderExamples.cs`

```csharp
// Get symbol and account
Symbol symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "EUR/USD");
Account account = Core.Instance.Accounts.FirstOrDefault(a => a.Name == "AccountName");

// Full parameter approach
var request = new PlaceOrderRequestParameters {
    Symbol = symbol,                    // Mandatory
    Account = account,                  // Mandatory
    Side = Side.Buy,                    // Mandatory (Buy or Sell)
    OrderTypeId = OrderType.Market,     // Mandatory
    Quantity = 0.5,                     // Mandatory

    // Optional parameters
    Price = 1.52,                       // Required for Limit and StopLimit
    TriggerPrice = 1.52,                // Required for Stop and StopLimit
    TrailOffset = 20,                   // Required for TrailingStop

    TimeInForce = TimeInForce.Day,      // Day, GTC, GTD, GTT, FOK, IOC
    ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1),

    StopLoss = SlTpHolder.CreateSL(1.4),
    TakeProfit = SlTpHolder.CreateTP(2.2)
};

TradingOperationResult result = Core.Instance.PlaceOrder(request);

// Check result
if (result.Status == TradingOperationResultStatus.Success) {
    // Order placed successfully
} else {
    // Handle failure
    string error = result.Message;
}
```

### Simplified Order Placement

```csharp
// Market order with quantity = 1
Core.Instance.PlaceOrder(symbol, account, Side.Buy);

// Limit order
Core.Instance.PlaceOrder(symbol, account, Side.Sell, price: 1.5);

// Stop order
Core.Instance.PlaceOrder(symbol, account, Side.Buy, triggerPrice: 2.1);

// Stop limit order
Core.Instance.PlaceOrder(
    symbol,
    account,
    Side.Sell,
    timeInForce: TimeInForce.GTC,
    quantity: 0.6,
    price: 2.2,
    triggerPrice: 2.1
);
```

### Multiple Stop Loss / Take Profit

**File Reference:** `Common/PlaceOrderWithMultipleSlTP.cs:36-51`

Place orders with multiple SL/TP levels (scaling out):

```csharp
var request = new PlaceOrderRequestParameters {
    Symbol = symbol,
    Account = account,
    Quantity = 3,  // Total quantity
    OrderTypeId = OrderType.Market,
    TimeInForce = TimeInForce.GTC
};

// Add multiple stop losses
request.StopLossItems.Add(SlTpHolder.CreateSL(
    10,                         // 10 pips offset
    PriceMeasurement.Offset,    // Offset from entry
    false,                      // Not trailing
    quantity: 2                 // Close 2 lots at this level
));
request.StopLossItems.Add(SlTpHolder.CreateSL(20, PriceMeasurement.Offset, false, quantity: 1));

// Add multiple take profits
request.TakeProfitItems.Add(SlTpHolder.CreateTP(15, PriceMeasurement.Offset, quantity: 2));
request.TakeProfitItems.Add(SlTpHolder.CreateTP(25, PriceMeasurement.Offset, quantity: 1));

Core.Instance.PlaceOrder(request);
```

### OCO Orders (One-Cancels-Other)

**File Reference:** `Common/LinkOrdersAsOCOExample.cs:10-22`

```csharp
public void LinkOCO(List<IOrder> orders) {
    Task.Run(() => {
        if (!orders.Any()) return;

        Core.Instance.SendCustomRequest(
            orders.First().ConnectionId,
            new LinkOCORequestParameters {
                OrdersToLink = orders
            }
        );
    });
}
```

### Adding SL/TP to Existing Position

**File Reference:** `Strategies/SetSlTpForOpenedPositionStrategy.cs:77-123`

```csharp
private void PlaceCloseOrder(Position position, CloseOrderType closeOrderType, double priceOffset) {
    var request = new PlaceOrderRequestParameters {
        Symbol = symbol,
        Account = account,
        Side = position.Side == Side.Buy ? Side.Sell : Side.Buy,
        Quantity = position.Quantity,
        PositionId = position.Id,
        AdditionalParameters = new List<SettingItem> {
            new SettingItemBoolean(OrderType.REDUCE_ONLY, true)
        }
    };

    if (closeOrderType == CloseOrderType.StopLoss) {
        var orderType = symbol.GetAlowedOrderTypes(OrderTypeUsage.CloseOrder)
            .FirstOrDefault(ot => ot.Behavior == OrderTypeBehavior.Stop);
        request.OrderTypeId = orderType.Id;
        request.TriggerPrice = position.Side == Side.Buy
            ? position.OpenPrice - priceOffset
            : position.OpenPrice + priceOffset;
    } else {
        var orderType = symbol.GetAlowedOrderTypes(OrderTypeUsage.CloseOrder)
            .FirstOrDefault(ot => ot.Behavior == OrderTypeBehavior.Limit);
        request.OrderTypeId = orderType.Id;
        request.Price = position.Side == Side.Buy
            ? position.OpenPrice + priceOffset
            : position.OpenPrice - priceOffset;
    }

    Core.Instance.PlaceOrder(request);
}
```

### Closing Positions

**File Reference:** `Strategies/SimpleMACross.cs:230-244`

```csharp
// Get all positions for symbol and account
var positions = Core.Instance.Positions.Where(
    x => x.Symbol == symbol && x.Account == account
).ToArray();

// Close each position
foreach (var position in positions) {
    var result = position.Close();

    if (result.Status == TradingOperationResultStatus.Failure) {
        // Handle error
        string error = result.Message;
    }
}
```

---

## Indicators

### Basic Indicator Structure

**File Reference:** `IndicatorExamples/SimpleMovingAverage.cs`

```csharp
using TradingPlatform.BusinessLayer;

public class SMA : Indicator {
    // Define parameters
    [InputParameter("Period of Simple Moving Average", 0, 1, 999, 1, 1)]
    public int Period = 10;

    [InputParameter("Sources prices for MA", 1, variants: new object[]{
        "Close", PriceType.Close,
        "Open", PriceType.Open,
        "High", PriceType.High,
        "Low", PriceType.Low,
        "Typical", PriceType.Typical,
        "Median", PriceType.Median,
        "Weighted", PriceType.Weighted
    })]
    public PriceType SourcePrice = PriceType.Close;

    // Constructor
    public SMA() : base() {
        Name = "Simple Moving Average";
        Description = "Average price for the last N periods";

        // Define output line(s)
        AddLineSeries("SMA", Color.Red, 1, LineStyle.Solid);

        SeparateWindow = false;  // Draw on main chart (true for separate window)
    }

    // Initialize (called after creation and parameter changes)
    protected override void OnInit() {
        ShortName = $"SMA ({Period}:{SourcePrice})";
    }

    // Main calculation (called on each bar update)
    protected override void OnUpdate(UpdateArgs args) {
        // args.Reason can be:
        // - HistoricalBar: Loading historical data
        // - NewTick: Real-time tick update
        // - NewBar: New bar just started

        if (Count <= Period)
            return;

        double sum = 0.0;
        for (int i = 0; i < Period; i++)
            sum += GetPrice(SourcePrice, i);

        // Set value for line 0 (default)
        SetValue(sum / Period);

        // For multiple lines, use:
        // SetValue(value, lineIndex);
    }
}
```

### Key Indicator Methods

```csharp
// Get price from historical data
double price = GetPrice(PriceType.Close, offset);  // offset=0 is current bar

// Get historical data directly
IHistoryItem bar = this.HistoricalData[offset];

// Set indicator value
SetValue(value);           // Set value for line 0
SetValue(value, lineIndex); // Set value for specific line

// Get indicator value
double currentValue = GetValue();           // Current value of line 0
double prevValue = GetValue(1);             // Previous value of line 0
double value = GetValue(offset, lineIndex); // Specific offset and line

// Access chart
if (this.CurrentChart != null) {
    // Chart is available
}

// Get symbol
Symbol symbol = this.Symbol;

// Total bars loaded
int barCount = this.Count;
```

### Using Built-In Indicators

**File Reference:** `IndicatorExamples/AccessIndicatorByName.cs:32-52`

```csharp
// Create indicator by name
IndicatorInfo emaInfo = Core.Instance.Indicators.All
    .FirstOrDefault(info => info.Name == "Exponential Moving Average");
Indicator emaIndicator = Core.Instance.Indicators.CreateIndicator(emaInfo);

// Set parameters
emaIndicator.Settings = new List<SettingItem> {
    new SettingItemInteger("MaPeriod", 10)
};

// Add to current indicator/strategy
AddIndicator(emaIndicator);

// Use built-in indicators directly
Indicator sma = Core.Instance.Indicators.BuiltIn.SMA(20, PriceType.Close);
Indicator ema = Core.Instance.Indicators.BuiltIn.EMA(10, PriceType.Close);
AddIndicator(sma);

// Get values
double smaValue = sma.GetValue();
double emaValue = ema.GetValue(0, 0);  // offset=0, line=0
```

### Indicator Chaining (Custom Data Source)

**File Reference:** `IndicatorExamples/DoubleSMAIndicator.cs:38-68`

```csharp
private Indicator sma;
private Indicator doubleSma;
private HistoricalDataCustom customData;

protected override void OnInit() {
    // First SMA on regular price data
    this.sma = Core.Instance.Indicators.BuiltIn.SMA(20, PriceType.Close);
    this.AddIndicator(this.sma);

    // Create custom historical data buffer
    this.customData = new HistoricalDataCustom(this);

    // Second SMA using first SMA as input (via custom data)
    this.doubleSma = Core.Instance.Indicators.BuiltIn.SMA(20, PriceType.Close);
    this.customData.AddIndicator(this.doubleSma);
}

protected override void OnUpdate(UpdateArgs args) {
    if (this.Period >= this.Count)
        return;

    // Get first SMA value
    var smaValue = this.sma.GetValue();

    // Feed it to custom data (as "Close" price)
    this.customData[PriceType.Close] = smaValue;

    // Get double-smoothed value
    var doubleSmaValue = this.doubleSma.GetValue();

    // Output both
    this.SetValue(smaValue, 0);
    this.SetValue(doubleSmaValue, 1);
}
```

### Custom Drawing on Chart

**File Reference:** `IndicatorExamples/DrawOnBars.cs:32-73`

```csharp
public override void OnPaintChart(PaintChartEventArgs args) {
    base.OnPaintChart(args);

    if (this.CurrentChart == null)
        return;

    Graphics graphics = args.Graphics;
    var mainWindow = this.CurrentChart.MainWindow;
    Font font = new Font("Arial", 10, FontStyle.Bold);

    // Get visible time range
    DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(
        mainWindow.ClientRectangle.Left
    );
    DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(
        mainWindow.ClientRectangle.Right
    );

    // Convert to bar indices
    int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
    int rightIndex = (int)Math.Ceiling(
        mainWindow.CoordinatesConverter.GetBarIndex(rightTime)
    );

    // Draw only visible bars
    for (int i = leftIndex; i <= rightIndex; i++) {
        if (i > 0 && i < this.HistoricalData.Count) {
            var bar = this.HistoricalData[i, SeekOriginHistory.Begin] as HistoryItemBar;
            if (bar != null) {
                bool isUp = bar.Close > bar.Open;

                // Convert price/time to screen coordinates
                int x = (int)Math.Round(
                    mainWindow.CoordinatesConverter.GetChartX(bar.TimeLeft)
                    + this.CurrentChart.BarsWidth / 2.0
                );
                int y = (int)Math.Round(
                    isUp
                        ? mainWindow.CoordinatesConverter.GetChartY(bar.High) - 20
                        : mainWindow.CoordinatesConverter.GetChartY(bar.Low) + 20
                );

                graphics.DrawString(
                    isUp ? "Up" : "Down",
                    font,
                    isUp ? Brushes.Green : Brushes.Red,
                    x, y
                );
            }
        }
    }
}
```

---

## Strategies

### Basic Strategy Structure

**File Reference:** `Strategies/SimpleMACross.cs`

```csharp
using TradingPlatform.BusinessLayer;

public class MyStrategy : Strategy, ICurrentAccount, ICurrentSymbol {
    // Parameters
    [InputParameter("Symbol", 0)]
    public Symbol CurrentSymbol { get; set; }

    [InputParameter("Account", 1)]
    public Account CurrentAccount { get; set; }

    [InputParameter("Period", 2, minimum: 1, maximum: 100, increment: 1)]
    public int Period { get; set; }

    // Monitor these connections for state changes
    public override string[] MonitoringConnectionsIds => new string[] {
        this.CurrentSymbol?.ConnectionId,
        this.CurrentAccount?.ConnectionId
    };

    private HistoricalData historicalData;
    private Indicator indicator;

    // Constructor
    public MyStrategy() : base() {
        this.Name = "My Strategy";
        this.Description = "Strategy description";
    }

    // Start strategy
    protected override void OnRun() {
        // Restore symbol if needed (for serialization)
        if (this.CurrentSymbol != null && this.CurrentSymbol.State == BusinessObjectState.Fake)
            this.CurrentSymbol = Core.Instance.GetSymbol(this.CurrentSymbol.CreateInfo());

        // Restore account
        if (this.CurrentAccount != null && this.CurrentAccount.State == BusinessObjectState.Fake)
            this.CurrentAccount = Core.Instance.GetAccount(this.CurrentAccount.CreateInfo());

        // Validate inputs
        if (this.CurrentSymbol == null || this.CurrentAccount == null) {
            this.Log("Invalid parameters", StrategyLoggingLevel.Error);
            return;
        }

        // Load historical data
        this.historicalData = this.CurrentSymbol.GetHistory(
            Period.MIN5,
            this.CurrentSymbol.HistoryType,
            DateTime.UtcNow.AddDays(-10)
        );

        // Create indicators
        this.indicator = Core.Instance.Indicators.BuiltIn.SMA(20, PriceType.Close);
        this.historicalData.AddIndicator(this.indicator);

        // Subscribe to events
        Core.PositionAdded += this.OnPositionAdded;
        Core.PositionRemoved += this.OnPositionRemoved;
        this.historicalData.HistoryItemUpdated += this.OnHistoryUpdate;
        this.CurrentSymbol.NewQuote += this.OnNewQuote;
    }

    // Stop strategy
    protected override void OnStop() {
        // Unsubscribe from events
        Core.PositionAdded -= this.OnPositionAdded;
        Core.PositionRemoved -= this.OnPositionRemoved;

        if (this.historicalData != null) {
            this.historicalData.HistoryItemUpdated -= this.OnHistoryUpdate;
            this.historicalData.Dispose();
        }

        if (this.CurrentSymbol != null)
            this.CurrentSymbol.NewQuote -= this.OnNewQuote;

        base.OnStop();
    }

    // Event handlers
    private void OnPositionAdded(Position position) {
        if (position.Symbol == this.CurrentSymbol && position.Account == this.CurrentAccount) {
            this.Log($"Position opened: {position.Side} {position.Quantity}");
        }
    }

    private void OnPositionRemoved(Position position) {
        this.Log($"Position closed");
    }

    private void OnHistoryUpdate(object sender, HistoryEventArgs e) {
        // Process bar update
        this.ProcessSignal();
    }

    private void OnNewQuote(Symbol symbol, Quote quote) {
        // Process real-time quote
    }

    private void ProcessSignal() {
        // Trading logic here
    }
}
```

### Strategy Logging

```csharp
// Log levels
this.Log("Info message");
this.Log("Trading message", StrategyLoggingLevel.Trading);
this.Log("Error message", StrategyLoggingLevel.Error);
```

### Strategy Metrics (Telemetry)

**File Reference:** `Strategies/SimpleMACross.cs:155-165`

```csharp
protected override void OnInitializeMetrics(Meter meter) {
    base.OnInitializeMetrics(meter);

    // Create observable counters for monitoring
    meter.CreateObservableCounter(
        "total-long-positions",
        () => this.longPositionsCount,
        description: "Total long positions"
    );

    meter.CreateObservableCounter(
        "total-pl-net",
        () => this.totalNetPl,
        description: "Total Net profit/loss"
    );
}
```

### MA Crossover Strategy Pattern

**File Reference:** `Strategies/SimpleMACross.cs:215-294`

```csharp
private void OnUpdate() {
    var positions = Core.Instance.Positions.Where(
        x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount
    ).ToArray();

    if (positions.Any()) {
        // Close positions on opposite signal
        if (this.fastMA.GetValue(1) < this.slowMA.GetValue(1) ||
            this.fastMA.GetValue(1) > this.slowMA.GetValue(1)) {

            foreach (var position in positions) {
                var result = position.Close();
                if (result.Status == TradingOperationResultStatus.Failure) {
                    this.Log($"Close failed: {result.Message}", StrategyLoggingLevel.Error);
                }
            }
        }
    } else {
        // Open long position (fast crosses above slow)
        if (this.fastMA.GetValue(2) < this.slowMA.GetValue(2) &&
            this.fastMA.GetValue(1) > this.slowMA.GetValue(1)) {

            var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters {
                Account = this.CurrentAccount,
                Symbol = this.CurrentSymbol,
                OrderTypeId = OrderType.Market,
                Quantity = this.Quantity,
                Side = Side.Buy
            });
        }

        // Open short position (fast crosses below slow)
        else if (this.fastMA.GetValue(2) > this.slowMA.GetValue(2) &&
                 this.fastMA.GetValue(1) < this.slowMA.GetValue(1)) {

            var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters {
                Account = this.CurrentAccount,
                Symbol = this.CurrentSymbol,
                OrderTypeId = OrderType.Market,
                Quantity = this.Quantity,
                Side = Side.Sell
            });
        }
    }
}
```

---

## Advanced Features

### Webhook Strategy

**File Reference:** `Common/Webhook_Strategy_Example.cs:32-104`

Create a strategy that listens for HTTP webhooks (e.g., from TradingView):

```csharp
public class WebhookStrategy : Strategy {
    private CancellationTokenSource cancellationToken;
    private event Action<string> OnWebhookMessageReceived;

    protected override void OnRun() {
        this.cancellationToken = new CancellationTokenSource();

        Thread webhookThread = new Thread(async () =>
            await this.StartListening(this.cancellationToken.Token)
        );
        webhookThread.Start();

        OnWebhookMessageReceived += this.ProcessWebhookMessage;
    }

    protected override void OnStop() {
        OnWebhookMessageReceived -= this.ProcessWebhookMessage;
        this.cancellationToken.Cancel();
    }

    private void ProcessWebhookMessage(string message) {
        Log($"Webhook received: {message}");
        // Parse JSON and execute trades
    }

    private async Task StartListening(CancellationToken token) {
        string url = "http://localhost:5000/";
        HttpListener listener = new HttpListener();

        try {
            listener.Prefixes.Add(url);
            listener.Start();
            Log("Webhook listener started");

            while (!token.IsCancellationRequested) {
                var contextTask = listener.GetContextAsync();
                var completedTask = await Task.WhenAny(
                    contextTask,
                    Task.Delay(Timeout.Infinite, token)
                );

                if (completedTask == contextTask) {
                    HttpListenerContext context = await contextTask;
                    HttpListenerRequest request = context.Request;

                    using (StreamReader reader = new StreamReader(
                        request.InputStream,
                        Encoding.UTF8
                    )) {
                        string message = reader.ReadToEnd();
                        OnWebhookMessageReceived?.Invoke(message);
                    }
                }
            }
        } finally {
            if (listener.IsListening) {
                listener.Stop();
                listener.Close();
            }
        }
    }
}
```

### VWAP Calculations

**File Reference:** `Common/VwapExamples.cs:20-128`

```csharp
// VWAP by period (e.g., 1-hour VWAP on 5-min bars)
var parameters = new HistoryAggregationVwapParameters {
    Aggregation = new HistoryAggregationTime(Period.MIN5),
    DataType = VwapDataType.CurrentTF,
    Period = Period.HOUR1,  // VWAP resets every hour
    PriceType = VwapPriceType.HLC3,
    StdCalculationType = VwapStdCalculationType.StandardDeviation,
    TimeZone = Core.Instance.TimeUtils.SelectedTimeZone
};

var vwapHistory = symbol.GetHistory(new HistoryRequestParameters {
    Aggregation = new HistoryAggregationVwap(parameters),
    Symbol = symbol,
    FromTime = DateTime.UtcNow.AddDays(-1),
    ToTime = default(DateTime),  // Real-time updates
    CancellationToken = cts.Token,
    HistoryType = symbol.HistoryType
});

// Process VWAP data
for (int i = 0; i < vwapHistory.Count; i++) {
    if (vwapHistory[i, SeekOriginHistory.Begin] is IVwapHistoryItem vwap) {
        double value = vwap.Value;
        double stdCoeff = vwap.STDCoefficient;
        double mpdCoeff = vwap.MPDCoefficient;
    }
}

// Subscribe to real-time VWAP updates
vwapHistory.NewHistoryItem += (sender, e) => {
    if (e.HistoryItem is IVwapHistoryItem vwap) {
        // New VWAP value
    }
};
```

### PowerTrades Analysis

**File Reference:** `Common/PowerTradesExamples.cs:17-99`

PowerTrades identifies significant volume clusters:

```csharp
var parameters = new HistoryAggregationPowerTradesParameters {
    TotalVolume = 100,           // Minimum total volume
    MinTradeVolume = 0,          // Minimum single trade volume
    MaxTradeVolume = 100000,     // Maximum single trade volume
    TimeInterval = 5,            // Time window (seconds)
    BasisVolumeInterval = 300,   // Basis volume calculation period
    MinZoneHeight = 0,           // Minimum price zone height
    MaxZoneHeight = 100000,      // Maximum price zone height
    DeltaFilter = 50,            // Delta percentage filter
    BasisRatioFilter = 0         // Basis ratio filter
};

var powerTradesHistory = symbol.GetHistory(new HistoryRequestParameters {
    Aggregation = new HistoryAggregationPowerTrades(parameters),
    FromTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddMinutes(-10),
    ToTime = default(DateTime),  // Real-time
    Symbol = symbol,
    CancellationToken = cts.Token,
    HistoryType = HistoryType.Last,
    Period = Period.TICK1
});

// Process PowerTrades
for (int i = 0; i < powerTradesHistory.Count; i++) {
    if (powerTradesHistory[i, SeekOriginHistory.Begin] is IPowerTradesHistoryItem pt) {
        double volume = pt.Cumulative;
        double deltaPercent = pt.DeltaPercent;
        double basisRatioPercent = pt.BasisRatioPercent;
    }
}
```

### Level2 with MBO (Market By Order)

**File Reference:** `IndicatorExamples/Level2WithMBOIndicator.cs:125-132`

```csharp
// Subscribe to Level2 updates
this.Symbol.NewLevel2 += this.OnNewLevel2;

// Get depth of market with individual orders (no aggregation)
var dom = this.Symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections(
    new GetDepthOfMarketParameters {
        GetLevel2ItemsParameters = new GetLevel2ItemsParameters {
            AggregateMethod = AggregateMethod.None,  // Disable aggregation for MBO
            LevelsCount = 100
        }
    }
);

// Process individual orders at each price level
foreach (var ask in dom.Asks) {
    double price = ask.Price;
    double size = ask.Size;
    // Each entry may represent individual order
}

foreach (var bid in dom.Bids) {
    double price = bid.Price;
    double size = bid.Size;
}
```

### Custom Order Placing Strategies

**File Reference:** `PlaceOrderStrategies/MarketIfTouchedOrderPlacingStrategy/`

Create custom order execution logic:

```csharp
public abstract class PlaceOrderIfTouchedOrderPlacingStrategy : OrderPlacingStrategy {
    private PlaceOrderRequestParameters placeOrderRequest;
    private CancellationTokenSource cts;

    protected override void OnPlaceOrder(PlaceOrderRequestParameters request) {
        this.placeOrderRequest = request;
        this.cts = new CancellationTokenSource();

        // Create visual local order (shown in UI)
        var localOrder = new LocalOrder {
            Symbol = request.Symbol,
            Account = request.Account,
            Side = request.Side,
            TotalQuantity = request.Quantity,
            OrderType = new CustomOrderType("MIT", request.OrderType),
            Price = request.Price
        };
        string localOrderId = Core.Instance.LocalOrders.AddOrder(localOrder);

        // Subscribe to price updates
        request.Symbol.NewLast += this.OnNewPrice;

        // Wait for trigger condition
        while (!this.finished && !this.cts.IsCancellationRequested)
            Thread.Sleep(100);

        // Cleanup
        Core.Instance.LocalOrders.RemoveOrder(localOrderId);
    }

    private void OnNewPrice(Symbol symbol, Last last) {
        // Check if price condition is met
        if (last.Price >= this.placeOrderRequest.Price) {
            // Execute actual order
            PlaceOrderRequestParameters marketRequest =
                (PlaceOrderRequestParameters)placeOrderRequest.Clone();
            marketRequest.OrderTypeId = OrderType.Market;
            Core.Instance.PlaceOrder(marketRequest);

            this.finished = true;
        }
    }

    protected override void OnCancel() => this.cts?.Cancel();
}
```

### Settings with Dynamic Visibility

**File Reference:** `Common/SettingsRelationsExample.cs:16-72`

```csharp
public override IList<SettingItem> Settings {
    get {
        var settings = base.Settings;

        var option1 = new SelectItem("First option", 1);
        var option2 = new SelectItem("Second option", 2);

        // Dropdown selector
        settings.Add(new SettingItemSelectorLocalized(
            "someSelectorField",
            new SelectItem("", this.someSelectorField),
            new List<SelectItem> { option1, option2 }
        ) {
            Text = "Selector",
            SortIndex = 10
        });

        // Field visible only when option1 is selected
        settings.Add(new SettingItemDouble("firstField", this.firstFieldValue) {
            Text = "First field",
            SortIndex = 20,
            Relation = new SettingItemRelationVisibility("someSelectorField", option1)
        });

        // Field visible only when option2 is selected
        settings.Add(new SettingItemDateTime("secondField", this.secondFieldValue) {
            Text = "Second field",
            SortIndex = 20,
            Relation = new SettingItemRelationVisibility("someSelectorField", option2)
        });

        // Field enabled only when option2 is selected
        settings.Add(new SettingItemString("thirdField", this.thirdFieldValue) {
            Text = "Third field",
            SortIndex = 30,
            Relation = new SettingItemRelationEnability("someSelectorField", option2)
        });

        return settings;
    }
    set {
        if (value.TryGetValue("someSelectorField", out int selectorValue))
            this.someSelectorField = selectorValue;
        // ... handle other fields
    }
}
```

---

## Code Reference Index

### Common Examples
- **PlaceOrderExamples.cs** - All order placement variations
- **HistoryExamples.cs** - Historical data loading and access
- **SubscribeMarketDataExamples.cs** - Real-time market data subscriptions
- **PlaceOrderWithMultipleSlTP.cs** - Multiple stop loss/take profit levels
- **Webhook_Strategy_Example.cs** - HTTP webhook listener strategy
- **PowerTradesExamples.cs** - Volume cluster analysis
- **VwapExamples.cs** - VWAP calculations (period and range)
- **SearchSymbolsExample.cs** - Symbol search functionality
- **LinkOrdersAsOCOExample.cs** - One-Cancels-Other order linking
- **AccessOrderTypeAdvancedParameters.cs** - Order type configuration
- **SettingsRelationsExample.cs** - Dynamic UI settings

### Indicator Examples
- **SimpleMovingAverage.cs** - Basic SMA implementation
- **ExponentialMovingAverage.cs** - EMA with exponential smoothing
- **DoubleSMAIndicator.cs** - Indicator chaining example
- **AccessIndicatorByName.cs** - Using built-in indicators
- **Level2WithMBOIndicator.cs** - Market depth visualization
- **DrawOnBars.cs** - Custom GDI drawing
- **AsynchronousIndicator.cs** - Async operations in indicators

### Strategy Examples
- **SimpleMACross.cs** - Complete MA crossover strategy (249 lines)
- **SetSlTpForOpenedPositionStrategy.cs** - Adding SL/TP to positions

### Order Placing Strategies
- **MarketIfTouchedOrderPlacingStrategy.cs** - MIT order type
- **PlaceOrderIfTouchedOrderPlacingStrategy.cs** - Base class for triggered orders
- **RepeatOrderPlacing.cs** - Repeated order placement with delays

---

## Key Concepts for AI Models

### Important Patterns

1. **Always check for null and validate inputs** - Especially in OnRun()
2. **Subscribe and unsubscribe from events properly** - Memory leaks are common
3. **Use right-to-left indexing** - history[0] is most recent
4. **Check Count before accessing history** - Avoid index out of range
5. **Handle trading operation results** - Always check TradingOperationResultStatus
6. **Use wait flags for async operations** - Prevent duplicate orders
7. **Restore symbol/account from fake state** - Required for serialization
8. **Dispose resources in OnClear/OnStop** - HistoricalData, Indicators

### Common Gotchas

1. **Historical data indexing** - Remember [0] = current, [1] = previous
2. **Event subscriptions** - Always unsubscribe in cleanup
3. **Symbol/Account state** - Check for BusinessObjectState.Fake and restore
4. **Order placement timing** - Use flags to prevent rapid-fire orders
5. **Connection validation** - Ensure symbol and account are from same connection
6. **UpdateArgs.Reason** - Handle HistoricalBar, NewTick, NewBar differently
7. **Thread safety** - Use proper synchronization for multi-threaded code

### Best Practices

1. **Logging** - Use appropriate log levels (Info, Trading, Error)
2. **Error handling** - Always check result status and handle failures
3. **Resource cleanup** - Dispose historical data and indicators
4. **Parameter validation** - Check inputs in OnRun before proceeding
5. **Performance** - Only process visible bars in OnPaintChart
6. **Memory management** - Unsubscribe from events, dispose objects
7. **Testing** - Use historical data first, then live paper trading

---

## Next Steps for Development

When working with this codebase:

1. **Start with Common examples** - Understand basic patterns
2. **Review SimpleMACross.cs** - Complete strategy template
3. **Test with historical data** - Use backtesting before live
4. **Use Core.Instance.Loggers** - Debug with proper logging
5. **Check official docs** - https://api.quantower.com/
6. **Visual Studio Extension** - Install for easier development

---

## Resources

- **Official API Documentation:** https://api.quantower.com/
- **GitHub Examples:** https://github.com/Quantower/Examples
- **Installation Guide:** https://help.quantower.com/quantower-algo/installing-visual-studio
- **Help Center:** https://help.quantower.com/

---

*This document was generated through code analysis on 2025-11-12. For the most up-to-date information, refer to the official Quantower API documentation.*
