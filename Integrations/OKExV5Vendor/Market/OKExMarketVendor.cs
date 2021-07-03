using OKExV5Vendor.API;
using OKExV5Vendor.API.OrderType;
using OKExV5Vendor.API.REST.Models;
using OKExV5Vendor.API.Websocket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace OKExV5Vendor.Market
{
    class OKExMarketVendor : Vendor
    {
        #region Parameters

        private readonly OKExMarketClient client;

        protected readonly IDictionary<string, OKExSymbol> allSymbolsCache;

        #endregion Parameters

        public OKExMarketVendor(OKExMarketClient client)
        {
            this.client = client;
            this.client.OnNewTrade += this.Client_OnNewTrade;
            this.client.OnNewMark += this.Client_OnNewMark;
            this.client.OnNewQuote += this.Client_OnNewQuote;
            this.client.OnNewTicker += this.Client_OnNewTicker;
            this.client.OnNewIndexTicker += this.Client_OnNewIndexTicker;
            this.client.OnNewIndexPrice += this.Client_OnNewIndexPrice;
            this.client.OnOrderBookSnapshot += this.Client_OnOrderBookSnapshot;
            this.client.OnOrderBookUpdate += this.Client_OnOrderBookUpdate;
            this.client.OnInstrumentChanged += this.Client_OnInstrumentChanged;
            this.client.OnFundingRateUpdated += this.Client_OnFundingRateUpdated;
            this.client.OnError += this.Client_OnError;

            this.allSymbolsCache = new Dictionary<string, OKExSymbol>();
        }

        #region Connection

        public override ConnectionResult Connect(ConnectRequestParameters connectRequestParameters)
        {
            if (connectRequestParameters.CancellationToken.IsCancellationRequested)
                return ConnectionResult.CreateCancelled();

            if (this.client.Connect(connectRequestParameters.CancellationToken, out string error))
            {
                var symbols = this.client.SymbolsProvider.GetSymbols(OKExInstrumentType.Spot, OKExInstrumentType.Swap, OKExInstrumentType.Futures, OKExInstrumentType.Option, OKExInstrumentType.Index).ToList();

                foreach (var s in symbols)
                    this.allSymbolsCache[s.UniqueInstrumentId] = s;

                return ConnectionResult.CreateSuccess();
            }
            else
                return ConnectionResult.CreateFail(error ?? "Unknown error");
        }
        public override void OnConnected(CancellationToken token)
        {
            this.client.SubscribeFundingRates();
            base.OnConnected(token);
        }
        public override PingResult Ping()
        {
            return new PingResult()
            {
                State = this.client.IsConnected ? PingEnum.Connected : PingEnum.Disconnected,
                PingTime = this.client.Ping
            };
        }
        public override void Disconnect()
        {
            if (this.client != null)
                this.client.Disconnect();

            base.Disconnect();
        }

        #endregion Connection

        #region Symbols

        public override MessageSymbolTypes GetSymbolTypes(CancellationToken token)
        {
            return new MessageSymbolTypes()
            {
                SymbolTypes = new SymbolType[]
                {
                    SymbolType.Crypto,
                    SymbolType.Swap,
                    SymbolType.Futures,
                    SymbolType.Options,
                    SymbolType.Indexes
                }
            };
        }
        public override IList<MessageSymbol> GetSymbols(CancellationToken token)
        {
            return this.allSymbolsCache.Values
                .Where(s => s.InstrumentType != OKExInstrumentType.Option)
                .Select(s => this.CreateSymbolMessage(s))
                .ToList();
        }
        public override IList<MessageAsset> GetAssets(CancellationToken token)
        {
            var uniqueAssets = new HashSet<string>();
            var list = new List<MessageAsset>();

            foreach (var symbol in this.allSymbolsCache.Values)
            {
                if (!uniqueAssets.Contains(symbol.ProductAsset))
                {
                    list.Add(this.CreateAssetMessage(symbol.ProductAsset));
                    uniqueAssets.Add(symbol.ProductAsset);
                }

                if (!uniqueAssets.Contains(symbol.QuottingAsset))
                {
                    list.Add(this.CreateAssetMessage(symbol.QuottingAsset));
                    uniqueAssets.Add(symbol.QuottingAsset);
                }
            }

            return list;
        }
        public override IList<MessageExchange> GetExchanges(CancellationToken token)
        {
            return new List<MessageExchange>()
            {
               new MessageExchange()
               {
                   ExchangeName = "Exchange",
                   Id = OKExConsts.DEFAULT_EXCHANGE_ID
               }
            };
        }

        public override IList<MessageSymbolInfo> SearchSymbols(SearchSymbolsRequestParameters requestParameters)
        {
            return this.allSymbolsCache.Values
                .Where(s => s.InstrumentType != OKExInstrumentType.Option && s.Name.Contains(requestParameters.FilterName, StringComparison.InvariantCultureIgnoreCase))
                .Select(s => this.CreateSymbolMessage(s))
                .Cast<MessageSymbolInfo>()
                .ToList();
        }
        public override IList<MessageSymbolInfo> GetFutureContracts(GetFutureContractsRequestParameters requestParameters)
        {
            string underlierId = requestParameters.UnderlierId ?? requestParameters.Root;

            if (underlierId == null || !this.client.SymbolsProvider.TryGetFuturesByUnderlier(underlierId, out var futures))
                return base.GetFutureContracts(requestParameters);

            return futures.Select(f => this.CreateSymbolMessage(f))
                .Cast<MessageSymbolInfo>()
                .ToList();
        }
        public override MessageSymbol GetNonFixedSymbol(GetSymbolRequestParameters requestParameters)
        {
            if (this.allSymbolsCache.TryGetValue(requestParameters.SymbolId, out var symbol))
                return this.CreateSymbolMessage(symbol);
            else
                return base.GetNonFixedSymbol(requestParameters);
        }
        public override IList<MessageOptionSerie> GetOptionSeries(GetOptionSeriesRequestParameters requestParameters)
        {
            if (this.client.SymbolsProvider.TryGetOptionsByUnderlier(requestParameters.UnderlierId, out var strikes))
            {
                return strikes.Select(s => s.ExpiryTimeUtc)
                    .Distinct()
                    .Select(s => new MessageOptionSerie()
                    {
                        ExchangeId = OKExConsts.DEFAULT_EXCHANGE_ID,
                        ExpirationDate = s,
                        UnderlierId = requestParameters.UnderlierId,
                        Id = requestParameters.UnderlierId + "_" + s
                    })
                    .ToList();
            }
            else
                return base.GetOptionSeries(requestParameters);
        }
        public override IList<MessageSymbolInfo> GetStrikes(GetStrikesRequestParameters requestParameters)
        {
            if (this.client.SymbolsProvider.TryGetOptionsByUnderlier(requestParameters.UnderlierId, out var strikes))
            {
                return strikes.Where(s => s.ExpiryTimeUtc == requestParameters.ExpirationDate)
                    .Select(s => this.CreateSymbolMessage(s))
                    .Cast<MessageSymbolInfo>()
                    .ToList();
            }
            else
                return base.GetStrikes(requestParameters);
        }

        #endregion Symbols

        #region History

        public override HistoryMetadata GetHistoryMetadata(CancellationToken cancelationToken)
        {
            return new HistoryMetadata()
            {
                AllowedHistoryTypes = new HistoryType[] { HistoryType.Last, HistoryType.Mark },
                AllowedPeriods = OKExConsts.AllowedPeriods,
                //DownloadingStep_Tick = TimeSpan.FromHours(1),
                DownloadingStep_Minute = TimeSpan.FromDays(1),
            };
        }
        public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters)
        {
            var result = new List<IHistoryItem>();

            if (!this.allSymbolsCache.TryGetValue(requestParameters.SymbolId, out var okexSymbol))
                return result;

            if (requestParameters.Period == Period.TICK1)
            {
                var ticks = this.client.GetTickHistory(okexSymbol, requestParameters.CancellationToken, out string error);

                if (!string.IsNullOrEmpty(error))
                {
                    this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(error));
                    return result;
                }

                long prevTickTime = default;

                for (int i = ticks.Length - 1; i >= 0; i--)
                {
                    var tick = ticks[i];

                    if (tick.Time >= requestParameters.FromTime && tick.Time <= requestParameters.ToTime)
                    {
                        var item = new HistoryItemLast()
                        {
                            Price = tick.Price ?? default,
                            Volume = tick.Size ?? default,
                            TicksLeft = tick.Time.Ticks
                        };

                        if (prevTickTime >= item.TicksLeft)
                            item.TicksLeft = prevTickTime + 1;

                        prevTickTime = item.TicksLeft;
                        result.Add(item);
                    }
                }
            }
            else
            {
                var okexPeriod = requestParameters.Period.ToOKEx();
                var historyType = requestParameters.HistoryType.ToOKEx();
                var after = requestParameters.ToTime;
                bool keepGoing = true;

                while (keepGoing)
                {
                    var candles = this.client.GetCandleHistory(okexSymbol, after, okexPeriod, historyType, requestParameters.CancellationToken, out string error);

                    if (!string.IsNullOrEmpty(error))
                    {
                        this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(error));
                        break;
                    }

                    foreach (var item in candles)
                    {
                        if (item.Time < requestParameters.FromTime)
                        {
                            keepGoing = false;
                            break;
                        }

                        if (requestParameters.ToTime >= item.Time)
                        {
                            result.Add(new HistoryItemBar()
                            {
                                Close = item.Close,
                                Open = item.Open,
                                High = item.High,
                                Low = item.Low,
                                TicksLeft = item.Time.Ticks,
                                Volume = (okexSymbol.IsInverseContractSymbol ? item.CurrencyVolume : item.Volume) ?? default
                            });
                        }

                        after = item.Time;
                    }

                    if (keepGoing)
                        keepGoing = candles.Length == 100;
                }
            }

            var sss = result.GroupBy(j => j.TicksLeft).Where(s => s.Count() > 1);


            return result;
        }

        #endregion History

        #region Subscription

        public override void SubscribeSymbol(SubscribeQuotesParameters parameters)
        {
            if (!this.allSymbolsCache.TryGetValue(parameters.SymbolId, out var okexSymbol))
                return;

            switch (parameters.SubscribeType)
            {
                case SubscribeQuoteType.Quote:
                    {
                        this.client.SubscribeQuote(okexSymbol);
                        break;
                    }
                case SubscribeQuoteType.Level2:
                    {
                        this.client.SubscribeLevel2(okexSymbol);
                        break;
                    }
                case SubscribeQuoteType.Last:
                    {
                        if (okexSymbol.InstrumentType == OKExInstrumentType.Index)
                            this.client.SubscribeIndexPrice(okexSymbol);
                        else
                            this.client.SubscribeLast(okexSymbol);
                        break;
                    }
                case SubscribeQuoteType.Mark:
                    {
                        this.client.SubscribeMark(okexSymbol);
                        break;
                    }
            }
        }
        public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters)
        {
            if (!this.allSymbolsCache.TryGetValue(parameters.SymbolId, out var okexSymbol))
                return;

            switch (parameters.SubscribeType)
            {
                case SubscribeQuoteType.Quote:
                    {
                        this.client.UnsubscribeQuote(okexSymbol);
                        break;
                    }
                case SubscribeQuoteType.Level2:
                    {
                        this.client.UnsubscribeLevel2(okexSymbol);
                        break;
                    }
                case SubscribeQuoteType.Last:
                    {
                        if (okexSymbol.InstrumentType == OKExInstrumentType.Index)
                            this.client.UnsubscribeIndexPrice(okexSymbol);
                        else
                            this.client.UnsubscribeLast(okexSymbol);
                        break;
                    }
                case SubscribeQuoteType.Mark:
                    {
                        this.client.UnsubscribeMark(okexSymbol);
                        break;
                    }
            }
        }

        #endregion Subscription

        #region Orders

        public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token)
        {
            return new List<OrderType>()
            {
                new OKExMarketOrderType(TimeInForce.Default, TimeInForce.IOC),
                new OKExLimitOrderType(TimeInForce.GTC, TimeInForce.FOK, TimeInForce.IOC),
            };
        }

        #endregion Orders

        #region Event handler

        private void Client_OnNewTrade(OKExSymbol symbol, OKExTradeItem trade)
        {
            this.PushMessage(new Last(symbol.UniqueInstrumentId, trade.Price.Value, symbol.ConvertSizeToBaseCurrency(trade), trade.Time)
            {
                AggressorFlag = trade.Side == OKExSide.Buy ? AggressorFlag.Buy : AggressorFlag.Sell
            });
        }
        private void Client_OnNewMark(OKExSymbol symbol, OKExMarkItem mark)
        {
            this.PushMessage(new Mark(symbol.UniqueInstrumentId, mark.Time, mark.MarkPrice ?? default));
        }
        private void Client_OnNewQuote(OKExSymbol symbol, IOKExQuote quote)
        {
            this.PushMessage(new Quote(symbol.UniqueInstrumentId, quote.BidPrice ?? default, quote.BidSize ?? default, quote.AskPrice ?? default, quote.AskSize ?? default, quote.Time));
        }
        private void Client_OnNewTicker(OKExSymbol symbol, OKExTicker ticker, bool isFirstMessage)
        {
            var daybar = new DayBar(symbol.UniqueInstrumentId, ticker.Time);

            if (isFirstMessage)
            {
                daybar.Ask = ticker.AskPrice ?? default;
                daybar.AskSize = ticker.AskSize ?? default;

                daybar.Bid = ticker.BidPrice ?? default;
                daybar.BidSize = ticker.BidSize ?? default;

                daybar.Last = ticker.LastPrice ?? default;
                daybar.LastSize = ticker.LastSize ?? default;
            }

            if (ticker.OpenPrice24h.HasValue)
                daybar.Open = ticker.OpenPrice24h.Value;

            if (ticker.LastPrice.HasValue)
            {
                daybar.Change = ticker.LastPrice.Value - ticker.OpenPriceUTC0.Value;
                daybar.ChangePercentage = ticker.LastPrice.Value * 100 / ticker.OpenPriceUTC0.Value - 100d;
            }

            if (ticker.HighPrice24h.HasValue)
                daybar.High = ticker.HighPrice24h.Value;

            if (ticker.LowPrice24h.HasValue)
                daybar.Low = ticker.LowPrice24h.Value;

            if (ticker.Volume24h.HasValue)
                daybar.Volume = ticker.Volume24h.Value;

            this.PushMessage(daybar);
        }
        private void Client_OnNewIndexPrice(OKExSymbol symbol, IOKExIndexPrice indexItem)
        {
            this.PushMessage(new Last(symbol.UniqueInstrumentId, indexItem.IndexPrice ?? default, 0, indexItem.Time));
        }
        private void Client_OnNewIndexTicker(OKExSymbol symbol, OKExIndexTicker ticker, bool isFirstMessage)
        {
            var daybar = new DayBar(symbol.UniqueInstrumentId, ticker.Time);

            if (isFirstMessage)
                daybar.Last = ticker.IndexPrice ?? default;

            if (ticker.OpenPrice24h.HasValue)
            {
                daybar.Open = ticker.OpenPrice24h.Value;

                if (ticker.IndexPrice.HasValue)
                {
                    daybar.Change = ticker.IndexPrice.Value - ticker.OpenPrice24h.Value;
                    daybar.ChangePercentage = ticker.IndexPrice.Value * 100 / ticker.OpenPrice24h.Value - 100d;
                }
            }

            if (ticker.HighPrice24h.HasValue)
                daybar.High = ticker.HighPrice24h.Value;

            if (ticker.LowPrice24h.HasValue)
                daybar.Low = ticker.LowPrice24h.Value;

            this.PushMessage(daybar);
        }
        private void Client_OnOrderBookUpdate(OKExSymbol symbol, OKExOrderBook book)
        {
            foreach (var item in book.Asks)
                this.PushMessage(this.CreateLevel2Item(QuotePriceType.Ask, symbol, item, book.Time));

            foreach (var item in book.Bids)
                this.PushMessage(this.CreateLevel2Item(QuotePriceType.Bid, symbol, item, book.Time));
        }
        private void Client_OnOrderBookSnapshot(OKExSymbol symbol, OKExOrderBook book)
        {
            var dom = new DOMQuote(symbol.UniqueInstrumentId, book.Time);

            foreach (var item in book.Asks)
                dom.Asks.Add(this.CreateLevel2Item(QuotePriceType.Ask, symbol, item, book.Time));

            foreach (var item in book.Bids)
                dom.Bids.Add(this.CreateLevel2Item(QuotePriceType.Bid, symbol, item, book.Time));

            this.PushMessage(dom);
        }
        private void Client_OnInstrumentChanged(OKExSymbol symbol)
        {
            this.allSymbolsCache[symbol.UniqueInstrumentId] = symbol;
            this.PushMessage(this.CreateSymbolMessage(symbol));
        }
        private void Client_OnFundingRateUpdated(OKExFundingRate rate)
        {
            if (this.allSymbolsCache.TryGetValue(rate.UniqueInstrumentId, out var okexSymbol))
                this.PushMessage(this.CreateSymbolMessage(okexSymbol, rate));
        }
        private void Client_OnError(string message)
        {
            this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(message));
        }

        #endregion Event handler

        #region Factory methods

        private MessageSymbol CreateSymbolMessage(OKExSymbol symbol)
        {
            var message = new MessageSymbol(symbol.UniqueInstrumentId)
            {
                Name = symbol.Name,
                Description = symbol.ProductAsset + " vs " + symbol.QuottingAsset,
                VariableTickList = new List<VariableTick>()
                {
                    new VariableTick(double.NegativeInfinity, double.PositiveInfinity, true, symbol.TickSize.Value, 1.0)
                },
                ProductAssetId = symbol.ProductAsset,
                HistoryType = HistoryType.Last,
                QuotingCurrencyAssetID = symbol.QuottingAsset,
                LotSize = symbol.LotSize ?? 1,
                LotStep = symbol.LotSize ?? symbol.ContractMultiplier ?? 1,
                MinLot = symbol.MinOrderSize ?? 1,
                ExchangeId = OKExConsts.DEFAULT_EXCHANGE_ID,
                ExpirationDate = symbol.ExpiryTimeUtc,
                SymbolType = symbol.InstrumentType.ToTerminal(),
                AllowCalculateRealtimeChange = false,
                AllowCalculateRealtimeTicks = false,
                AllowCalculateRealtimeVolume = false,
                AllowAbbreviatePriceByTickSize = true,
                SymbolAdditionalInfo = new List<AdditionalInfoItem>()
            };

            if (message.SymbolType == SymbolType.Indexes)
            {
                if (this.client.SymbolsProvider.TryGetOptionsByUnderlier(symbol.OKExInstrumentId, out _))
                    message.AvailableOptions = AvailableDerivatives.Present;

                if (this.client.SymbolsProvider.TryGetFuturesByUnderlier(symbol.OKExInstrumentId, out _))
                    message.AvailableFutures = AvailableDerivatives.Present;
            }

            if (message.SymbolType == SymbolType.Futures)
            {
                message.Root = symbol.Underlier;
                message.UnderlierId = symbol.Underlier;
            }

            if (message.SymbolType == SymbolType.Options)
            {
                message.UnderlierId = symbol.Underlier;
                message.OptionType = symbol.OptionType.ToTerminal();
                message.StrikePrice = symbol.StrikePrice.Value;
                message.OptionSerieId = message.UnderlierId + "_" + message.ExpirationDate;
            }

            if (symbol.Leverage.HasValue)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem()
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    Id = "leverage",
                    NameKey = "Leverage",
                    ToolTipKey = "Leverage",
                    DataType = ComparingType.String,
                    Value = symbol.Leverage.Value.ToString(),
                    SortIndex = 100,
                });
            }

            if (symbol.FutureAlias != OKExFutureAliasType.Undefined)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem()
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    Id = "alias",
                    NameKey = "Alias",
                    ToolTipKey = "Alias",
                    DataType = ComparingType.String,
                    Value = symbol.FutureAlias.GetDescription(),
                    SortIndex = 100,
                });
            }

            if (symbol.Status != OKExInstrumentStatus.Undefined)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem()
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    Id = "status",
                    NameKey = "Status",
                    ToolTipKey = "Status",
                    DataType = ComparingType.String,
                    Value = symbol.Status.GetEnumMember(),
                    SortIndex = 100,
                });
            }

            return message;
        }
        private MessageSymbol CreateSymbolMessage(OKExSymbol symbol, OKExFundingRate rate)
        {
            var message = this.CreateSymbolMessage(symbol);

            if (message.SymbolAdditionalInfo == null)
                message.SymbolAdditionalInfo = new List<AdditionalInfoItem>();

            if (rate.FundingRate.HasValue)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem()
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    Id = "funding_rate",
                    NameKey = "Funding rate",
                    ToolTipKey = "Funding rate",
                    DataType = ComparingType.Double,
                    Value = rate.FundingRate.Value,
                    SortIndex = 100,
                });
            }
            if (rate.NextFundingRate.HasValue)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem()
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    Id = "next_funding_rate",
                    NameKey = "Next funding rate",
                    ToolTipKey = "Next funding rate",
                    DataType = ComparingType.Double,
                    Value = rate.NextFundingRate.Value,
                    SortIndex = 100,
                });
            }
            if (rate.FundingTime != default)
            {
                message.SymbolAdditionalInfo.Add(new AdditionalInfoItem()
                {
                    GroupInfo = OKExConsts.TRADING_INFO_GROUP,
                    Id = "funding_time",
                    NameKey = "Funding time",
                    ToolTipKey = "Funding time",
                    DataType = ComparingType.DateTime,
                    Value = rate.FundingTime,
                    SortIndex = 100,
                });
            }

            return message;
        }
        private MessageAsset CreateAssetMessage(string assetId)
        {
            return new MessageAsset()
            {
                Id = assetId,
                Name = assetId,
                MinimumChange = 1E-08
            };
        }
        private Level2Quote CreateLevel2Item(QuotePriceType priceType, OKExSymbol symbol, OKExOrderBookItem bookItem, DateTime time)
        {
            var id = new StringBuilder()
                .Append(symbol.UniqueInstrumentId)
                .Append("_")
                .Append(priceType == QuotePriceType.Ask ? "ask" : "bid")
                .Append("_")
                .Append(bookItem.Price);

            return new Level2Quote(priceType, symbol.UniqueInstrumentId, id.ToString(), bookItem.Price, symbol.ConvertSizeToBaseCurrency(bookItem), time)
            {
                Closed = bookItem.Size == 0
            };
        }

        #endregion Factory methods
    }

}
