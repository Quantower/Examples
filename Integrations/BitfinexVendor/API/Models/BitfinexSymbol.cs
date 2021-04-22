// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using Newtonsoft.Json;

namespace BitfinexVendor.API.Models
{
    class BitfinexSymbol
    {
        [JsonProperty("pair")]
        public string Pair
        {
            get => this.pair;
            private set => this.pair = value?.ToUpper();
        }
        private string pair;

        [JsonProperty("price_precision")]
        public int PricePrecision { get; private set; }

        [JsonProperty("initial_margin")]
        public double InitialMargin { get; private set; }

        [JsonProperty("minimum_margin")]
        public double MinimumMargin { get; private set; }

        [JsonProperty("maximum_order_size")]
        public double MaximumOrderSize { get; private set; }

        [JsonProperty("minimum_order_size")]
        public double MinimumOrderSize { get; private set; }

        [JsonProperty("expiration")]
        public string Expiration { get; private set; }

        [JsonProperty("margin")]
        public bool AllowMargin { get; private set; }
    }
}
