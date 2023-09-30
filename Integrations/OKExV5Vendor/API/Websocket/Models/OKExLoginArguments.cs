// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models;

[Obfuscation(Exclude = true)]
class OKExLoginArguments
{
    [JsonProperty("apiKey")]
    public string ApiKey { get; set; }

    [JsonProperty("passphrase")]
    public string Passphrase { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("sign")]
    public string Sigh { get; set; }
}