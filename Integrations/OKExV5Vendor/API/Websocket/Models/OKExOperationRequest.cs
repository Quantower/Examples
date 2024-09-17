// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExOperationRequest<T>
{
    [JsonProperty("op")]
    public string Op { get; set; }

    [JsonProperty("args")]
    public T[] Args { get; set; }
}