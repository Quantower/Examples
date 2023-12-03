using System.Linq;
using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class SearchSymbolsExample
    {
        public void SearchSymbol(Connection connection, string symbolName)
        {
            // Make a request to API for symbol searching
            var symbolsSearchResult = Core.Instance.SearchSymbols(new SearchSymbolsRequestParameters()
            {
                FilterName = symbolName,
                SymbolTypes = new[] { SymbolType.Futures },
                ConnectionId = connection.Id,
                ExchangeIds = connection.BusinessObjects.Exchanges.Select(x => x.Id).ToArray()
            });

            Core.Instance.Loggers.Log($"{symbolsSearchResult.Count} symbols were found for requested symbol name.");

            // When we found required symbol in symbols search result, we need to request full symbol information.
            // After that it will be available in Core.Instance.Symbols
            if (symbolsSearchResult?.FirstOrDefault(x => x.Name == symbolName) is Symbol result)
            {
                Symbol symbol = Core.Instance.GetSymbol(new GetSymbolRequestParameters()
                {
                    SymbolId = result.Id
                });
            }
        }
    }
}