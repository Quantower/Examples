using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExTicker : OKExTickerBase, IOKExQuote
    {
        [JsonProperty("instType")]
        public OKExInstrumentType InstrumentType { get; set; }

        [JsonProperty("last", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? LastPrice { get; set; }

        [JsonProperty("lastSz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? LastSize { get; set; }

        [JsonProperty("askPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? AskPrice { get; set; }

        [JsonProperty("askSz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? AskSize { get; set; }

        [JsonProperty("bidPx", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? BidPrice { get; set; }

        [JsonProperty("bidSz", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? BidSize { get; set; }

        [JsonProperty("volCcy24h", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? VolumeCurrency24h { get; set; }

        [JsonProperty("vol24h", ItemConverterType = typeof(JsonStringToDoubleOrDefaultConverter))]
        public double? Volume24h { get; set; }
    }
}
