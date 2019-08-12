using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Events
{
    public class EventXRPCArgs : System.EventArgs
    {
        public EventXRPCArgs(XRPCServer server)
        {
            Server = server;
        }

        public XRPCServer Server { get; private set; }
    }
}
