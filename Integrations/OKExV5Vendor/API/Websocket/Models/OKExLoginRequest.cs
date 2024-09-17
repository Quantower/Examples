// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExLoginRequest : OKExOperationRequest<OKExLoginArguments>
{
    public OKExLoginRequest() => this.Op = "login";
}