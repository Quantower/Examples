// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bitfinex.API.Misc;

internal class BitfinexAuthHelper
{
    public long NonceOffset { get; set; }

    public static string GetPayload(long nonce) => $"AUTH{nonce}";

    public static async Task<string> GetPayload(HttpRequestMessage request, long nonce)
    {
        string apiPath = request.RequestUri.LocalPath.Trim('/');
        string body = request.Content != null ? await request.Content.ReadAsStringAsync() : null;
        return $"/api/{apiPath}{nonce}{body}";
    }

    public static string ComputeSignature(string payload, string apiSecret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(payload);
        byte[] secretBytes = Encoding.UTF8.GetBytes(apiSecret);

        using var hmac = new HMACSHA384(secretBytes);
        byte[] hash = hmac.ComputeHash(keyBytes);

        var builder = new StringBuilder();

        foreach (byte b in hash)
            builder.Append(b.ToString("X2"));

        return builder.ToString().ToLower();
    }

    public long GenerateNonce() => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() + this.NonceOffset;
}