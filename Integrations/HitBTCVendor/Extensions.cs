// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using HitBTC.Net.Models;
using System.Text;

namespace HitBTCVendor;

internal static class Extensions
{
    public static string Format(this HitError hitError)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(hitError.Message))
            sb.Append(hitError.Message.TrimEnd('.')).Append(". ");

        if (!string.IsNullOrEmpty(hitError.Description))
            sb.Append(hitError.Description);

        if (sb.Length == 0)
            sb.Append(hitError.Code);

        if (sb.Length == 0)
            sb.Append("Unformatted error");

        return sb.ToString();
    }
}