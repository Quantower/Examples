// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;

namespace OKExV5Vendor.API.Misc
{
    interface IPaginationLoadingItem
    {
        public string AfterId { get; }
        public DateTime Time { get; }
    }
}
