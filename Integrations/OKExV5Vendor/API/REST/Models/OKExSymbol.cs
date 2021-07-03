using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExSymbol : OKExSymbolBasedObject
    {
        [JsonProperty("instType")]
        public override OKExInstrumentType InstrumentType
        {
            get => this.instType;
            set
            {
                this.instType = value;
                this.RepopulateAdditionalFlags();
            }
        }
        private OKExInstrumentType instType = OKExInstrumentType.Any;

        [JsonProperty("instId")]
        public override string OKExInstrumentId { get; set; }

        [JsonProperty("uly")]
        public string Underlier
        {
            get => this.underlier;
            set
            {
                this.underlier = value;
                this.RepopulateAdditionalFlags();
            }
        }
        private string underlier;

        [JsonConverter(typeof(JsonStringToIntOrNullConverter))]
        [JsonProperty("category")]
        public int? Category { get; set; }

        [JsonProperty("baseCcy")]
        public string BaseCurrency { get; set; }

        [JsonProperty("quoteCcy")]
        public string QuoteCurrency { get; set; }

        [JsonProperty("settleCcy")]
        public string SettlementCurrency { get; set; }

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("ctVal")]
        public double? ContractValue { get; set; }

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("ctMult")]
        public double? ContractMultiplier { get; set; }

        [JsonProperty("ctValCcy")]
        public string ContractValueCurrency { get; set; }

        [JsonConverter(typeof(JsonStringToEnumOrDefaultConverter))]
        [JsonProperty("optType")]
        public OKExOptionType OptionType { get; set; }

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("stk")]
        public double? StrikePrice { get; set; }

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("listTime")]
        public long? ListingTimeUnix { get; set; }
        public DateTime ListingTimeUtc => this.ListingTimeUnix.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this.ListingTimeUnix.Value).UtcDateTime : default;

        [JsonConverter(typeof(JsonStringToLongOrDefaultConverter))]
        [JsonProperty("expTime")]
        public long? ExpiryTimeUnix { get; set; }
        public DateTime ExpiryTimeUtc => this.ExpiryTimeUnix.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(this.ExpiryTimeUnix.Value).UtcDateTime : default;

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("lever")]
        public double? Leverage { get; set; }

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("tickSz")]
        public double? TickSize { get; set; }

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("lotSz")]
        public double? LotSize { get; set; }

        [JsonConverter(typeof(JsonStringToDoubleOrDefaultConverter))]
        [JsonProperty("minSz")]
        public double? MinOrderSize { get; set; }

        [JsonConverter(typeof(JsonStringToEnumOrDefaultConverter))]
        [JsonProperty("ctType")]
        public OKExContractType ContractType
        {
            get => this.contractType;
            set
            {
                this.contractType = value;
                this.RepopulateAdditionalFlags();
            }
        }
        private OKExContractType contractType;

        [JsonConverter(typeof(JsonStringToEnumOrDefaultConverter))]
        [JsonProperty("alias")]
        public OKExFutureAliasType FutureAlias { get; set; }

        [JsonConverter(typeof(JsonStringToEnumOrDefaultConverter))]
        [JsonProperty("state")]
        public OKExInstrumentStatus Status { get; set; }

        #region Additional flags

        public bool HasUnderlier { get; private set; }
        public bool IsInverseContractSymbol { get; private set; }

        public string ProductAsset => this.InstrumentType == OKExInstrumentType.Spot ? this.BaseCurrency : this.SettlementCurrency;
        public string QuottingAsset => this.InstrumentType == OKExInstrumentType.Spot ? this.QuoteCurrency : this.ContractValueCurrency;

        public string Name
        {
            get => this.name ?? this.OKExInstrumentId;
            set => this.name = value;
        }
        private string name;

        #endregion Additional flags

        private void RepopulateAdditionalFlags()
        {
            this.IsInverseContractSymbol = this.ContractType == OKExContractType.Inverse && (this.InstrumentType == OKExInstrumentType.Futures || this.InstrumentType == OKExInstrumentType.Swap || this.InstrumentType == OKExInstrumentType.Option);
            this.HasUnderlier = !string.IsNullOrEmpty(this.Underlier);
        }

        public override string ToString()
        {
            return this.OKExInstrumentId + " " + this.InstrumentType;
        }
    }
}
