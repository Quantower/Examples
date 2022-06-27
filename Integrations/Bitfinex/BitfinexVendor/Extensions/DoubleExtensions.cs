// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System.Globalization;

namespace BitfinexVendor.Extensions
{
    internal static class DoubleExtensions
    {
        public static string FormatPrice(this double value) => value.ToString("0.##########", CultureInfo.InvariantCulture);
    }
}