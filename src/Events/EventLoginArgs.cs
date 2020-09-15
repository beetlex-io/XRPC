using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Events
{
    public class EventLoginArgs : EventXRPCArgs
    {
        public EventLoginArgs(XRPCServer server, ISession session) : base(server)
        {
            Session = session;
        }

        public ISession Session { get; private set; }

        public string UserName { get; internal set; }

        public string Password { get; internal set; }

        public bool Success { get; set; } = true;

        public string Message { get; set; }
    }
}
