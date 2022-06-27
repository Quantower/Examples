// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

namespace BitfinexVendor.Extensions
{
    internal static class DecimalExtensions
    {
        public static double ToDouble(this decimal? value) => (double)(value ?? 0m);
    }
}