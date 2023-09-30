// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExUnsubscribeRequest : OKExOperationRequest<OKExChannelRequest>
{
    public OKExUnsubscribeRequest() =>
        this.Op = "unsubscribe";
}