using OKExV5Vendor.API.REST;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExLoginRequest : OKExOperationRequest<OKExLoginArguments>
    {
        public OKExLoginRequest()
            => this.Op = "login";
    }
}
