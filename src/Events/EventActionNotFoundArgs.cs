using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Events
{
    public class EventActionNotFoundArgs : EventXRPCArgs
    {
        public EventActionNotFoundArgs(XRPCServer server) : base(server
            )
        { }
    }
}
