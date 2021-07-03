using Newtonsoft.Json;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExOperationRequest<T>
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("args")]
        public T[] Args { get; set; }
    }
}
