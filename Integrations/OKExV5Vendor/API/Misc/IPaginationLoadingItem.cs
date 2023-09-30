// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;

namespace OKExV5Vendor.API.Misc;

internal interface IPaginationLoadingItem
{
    public string AfterId { get; }
    public DateTime Time { get; }
}