// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using OKExV5Vendor.API.Misc;
using OKExV5Vendor.API.REST.JsonConverters;
using System;

namespace OKExV5Vendor.API.REST.Models;

[JsonConverter(typeof(JsonCandleItemConverter))]
internal class OKExCandleItem : IPaginationLoadingItem
{
    public DateTime Time { get; set; }

    public double Open { get; set; }
    public double Close { get; set; }
    public double High { get; set; }
    public double Low { get; set; }

    public double? Volume { get; set; }
    public double? CurrencyVolume { get; set; }

    public string AfterId => null;
}