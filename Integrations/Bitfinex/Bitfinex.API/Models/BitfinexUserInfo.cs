// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;

namespace Bitfinex.API.Models;

public class BitfinexUserInfo
{
    public long Id { get; internal set; }

    public string Email { get; internal set; }

    public string Username { get; internal set; }

    public DateTime CreationTime { get; internal set; }

    public bool IsVerified { get; internal set; }

    public int VerificationLevel { get; internal set; }

    public string Timezone { get; internal set; }

    public string Locale { get; internal set; }

    public string Company { get; internal set; }

    public bool IsMerchantEnabled { get; internal set; }
}