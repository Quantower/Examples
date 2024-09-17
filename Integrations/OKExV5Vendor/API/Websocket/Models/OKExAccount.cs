// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.REST.JsonConverters;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExAccount
{
    [JsonProperty("uid")]
    public string Id { get; set; }

    [JsonProperty("acctLv", ItemConverterType = typeof(JsonStringToIntOrNullConverter))]
    internal int? _accountLevel;
    public OKExAccountLevel AccountLevel => _accountLevel.HasValue ? (OKExAccountLevel)_accountLevel.Value : OKExAccountLevel.Simple;

    [JsonProperty("posMode")]
    public OKExPositionMode PositionMode { get; set; }

    [JsonProperty("autoLoan")]
    public bool AutoLoan { get; set; }

    [JsonProperty("greeksType")]
    public OKExGreeksType GreeksType { get; set; }

    [JsonProperty("level")]
    public string Level { get; set; }

    [JsonProperty("levelTmp")]
    public string LevelTmp { get; set; }

    public bool IsLeverageSupported => this.AccountLevel != OKExAccountLevel.Undefined && this.AccountLevel != OKExAccountLevel.PortfolioMargin;
}