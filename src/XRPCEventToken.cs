using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public class XRPCEventToken
    {
        public XRPCEventToken()
        {

        }

        public ISession Session { get; internal set; }

        public XRPCServer Server { get; internal set; }

        public RPCPacket Request { get; internal set; }

        public override string ToString()
        {
            return $"{Request.Session.RemoteEndPoint}";
        }
    }
}
