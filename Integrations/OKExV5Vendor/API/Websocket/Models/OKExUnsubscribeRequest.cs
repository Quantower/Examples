using System.Reflection;

namespace OKExV5Vendor.API.Websocket.Models
{
    [Obfuscation(Exclude = true)]
    class OKExUnsubscribeRequest : OKExOperationRequest<OKExChannelRequest>
    {
        public OKExUnsubscribeRequest() =>
            this.Op = "unsubscribe";
    }
}
