using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX.Buffers;
using BeetleX.Clients;

namespace BeetleX.XRPC.Packets
{
    public class ClientPacket : BeetleX.Clients.IClientPacket
    {
        private RPCPacket mPacket;

        public Options Options { get; set; }

        public EventClientPacketCompleted Completed { get; set; }

        public IClientPacket Clone()
        {
            ClientPacket result = new ClientPacket();
            result.Options = this.Options;
            return result;
        }

        public void Decode(IClient client, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            while (true)
            {
                if (mPacket == null)
                {
                    mPacket = new RPCPacket();
                    mPacket.Client = (AsyncTcpClient)client;
                }
                if (mPacket.Read(Options, pstream))
                {
                    try
                    {
                        Completed?.Invoke(client, mPacket);
                    }
                    finally
                    {
                        mPacket = null;
                    }
                }
                else
                    return;
            }
        }

        public void Dispose()
        {

        }

        public void Encode(object data, IClient client, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            RPCPacket request = data as RPCPacket;
            request.Write(Options, pstream);
        }
    }
}
