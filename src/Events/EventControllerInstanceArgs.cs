using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Events
{
    public class EventControllerInstanceArgs : EventXRPCArgs
    {
        public EventControllerInstanceArgs(XRPCServer server,Type type) : base(server) {
            Type = type;
        }

        public Type Type { get; internal set; }

        public object Controller { get; set; }
    }
}
