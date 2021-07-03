namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExUnsubscribeRequest : OKExOperationRequest<OKExChannelRequest>
    {
        public OKExUnsubscribeRequest() =>
            this.Op = "unsubscribe";
    }
}
