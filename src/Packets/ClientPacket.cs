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
        private Response mResponse;

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
                if (mResponse == null)
                {
                    mResponse = new Response();
                    mResponse.Client = client;
                 
                }
                if (mResponse.Read(Options, pstream))
                {
                    try
                    {
                        Completed?.Invoke(client, mResponse);
                    }
                    finally
                    {
                        mResponse = null;
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
            Request request = data as Request;
            request.Write(Options, pstream);
        }
    }
}
