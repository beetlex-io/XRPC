using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Events
{
    public class EventPacketArgs : EventXRPCArgs
    {
        public EventPacketArgs(XRPCServer server, RPCPacket packet) : base(server
            )
        {
            Packet = packet;
        }

        public RPCPacket Packet { get; internal set; }
    }

    public class EventPacketProcessingArgs : EventPacketArgs
    {
        public EventPacketProcessingArgs(XRPCServer server, RPCPacket packet) : base(server, packet)
        {

        }

        public bool Cancel { get; set; } = false;
    }

}
