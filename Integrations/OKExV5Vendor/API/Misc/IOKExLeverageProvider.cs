// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using OKExV5Vendor.API.REST.Models;

namespace OKExV5Vendor.API.Misc;

internal interface IOKExLeverageProvider
{
    void PopulateLeverage(OKExSymbol symbol);
}