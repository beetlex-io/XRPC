using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX.Buffers;
using BeetleX.EventArgs;

namespace BeetleX.XRPC.Packets
{
    public class ServerPacket : IPacket
    {

        private RPCPacket mRequest;

        private PacketDecodeCompletedEventArgs mCompletedArgs = new PacketDecodeCompletedEventArgs();

        public EventHandler<PacketDecodeCompletedEventArgs> Completed { get; set; }

        public IPacket Clone()
        {
            ServerPacket packet = new ServerPacket();
            packet.Options = this.Options;
            return packet;
        }

        public Options Options { get; internal set; }

        public void Decode(ISession session, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            while (true)
            {
                if (mRequest == null)
                {
                    mRequest = new RPCPacket();
                    mRequest.Session = session;
                  
                }
                if (mRequest.Read(Options, pstream))
                {
                    mCompletedArgs.SetInfo(session, mRequest);
                    try
                    {
                        Completed?.Invoke(this, mCompletedArgs);
                    }
                    finally
                    {
                      
                        mRequest = null;
                    }
                }
                else
                    return;
            }
        }

        public void Dispose()
        {
            mCompletedArgs = null;

        }

        public void Encode(object data, ISession session, Stream stream)
        {
            OnEncode(session, data, stream);
        }


        private void OnEncode(ISession session, object data, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            RPCPacket response = data as RPCPacket;
            response.Write(Options, pstream);
        }


        public byte[] Encode(object data, IServer server)
        {
            byte[] result = null;
            using (Buffers.PipeStream stream = new PipeStream(server.SendBufferPool.Next(), server.Options.LittleEndian, server.Options.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                result = new byte[stream.Length];
                stream.Read(result, 0, result.Length);
            }
            return result;
        }

        public ArraySegment<byte> Encode(object data, IServer server, byte[] buffer)
        {
            using (Buffers.PipeStream stream = new PipeStream(server.SendBufferPool.Next(), server.Options.LittleEndian, server.Options.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                int count = (int)stream.Length;
                stream.Read(buffer, 0, count);
                return new ArraySegment<byte>(buffer, 0, count);
            }
        }
    }
}
