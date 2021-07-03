using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.Websocket.Models
{
    class OKExSubscribeRequest : OKExOperationRequest<OKExChannelRequest>
    {
        public OKExSubscribeRequest() => 
            this.Op = "subscribe";
    }
}
