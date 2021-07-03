using System;

namespace OKExV5Vendor.API.Misc
{
    interface IPaginationLoadingItemWithTime : IPaginationLoadingItem
    {
        public DateTime Time { get; }
    }
}
