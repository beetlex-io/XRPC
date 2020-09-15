using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public class RPCPacket : IDisposable
    {

        public RPCPacket()
        {
            Status = (short)StatusCode.SUCCESS;
        }

        public bool NeedReply { get; set; } = false;

        private int mRetain;

        private bool mBufferData = false;

        public int ID { get; set; }

        public string Url { get; set; }

        public Dictionary<string, string> Header
        {
            get; set;
        }

        public short Status { get; set; }

        public int Length { get; internal set; }

        public int Paramters { get; internal set; }

        public object[] Data { get; set; }

        public Type[] ResultType { get; set; }

        public ArraySegment<byte>? PayloadData { get; internal set; }

        public BeetleX.ISession Session { get; internal set; }

        public BeetleX.Clients.AsyncTcpClient Client { get; internal set; }

        public int ContentLength { get; internal set; }

        public override string ToString()
        {
            return Url;
        }

        private Options mOption;

        public bool Read(Options rpcOption, PipeStream stream)
        {
            mOption = rpcOption;
            if (Length == 0)
            {
                if (stream.Length > 4)
                {

                    Length = stream.ReadInt32();
                    if (Length > rpcOption.MessageMaxLength)
                    {
                        throw new BXException("The message to long!");
                    }
                }
            }
            if (Length > 0)
            {
                if (stream.Length >= Length)
                {
                    mRetain = (int)(stream.Length - Length);
                    try
                    {
                        uint id = stream.ReadUInt32();
                        this.NeedReply = (id >> 30) > 0;
                        ID = (int)(id << 4 >> 4);
                        Url = stream.ReadShortUTF();
                        Status = stream.ReadInt16();
                        int hs = stream.ReadByte();
                        Header = new Dictionary<string, string>();
                        if (hs > 0)
                        {
                            for (int i = 0; i < hs; i++)
                            {
                                string name = stream.ReadShortUTF();
                                string value = stream.ReadShortUTF();
                                Header[name] = value;
                            }
                        }
                        Paramters = stream.ReadByte();
                        ContentLength = (int)stream.Length - mRetain;
                        if (ContentLength > 0)
                        {
                            var data = rpcOption.PopBuffer(ContentLength);
                            stream.Read(data, 0, ContentLength);
                            PayloadData = new ArraySegment<byte>(data, 0, ContentLength);
                        }
                    }
                    catch (Exception e_)
                    {
                        throw new BXException($"Read protocol data error {e_.Message}", e_);
                    }

                    return true;
                }

            }
            return false;
        }

        public void Write(Options rpcOption, PipeStream stream)
        {
            mOption = rpcOption;
            try
            {
                var head = stream.Allocate(4);
                var postion = stream.CacheLength;
                uint id = (uint)ID;
                if (NeedReply)
                    id |= 1 << 30;
                stream.Write(id);
                stream.WriteShortUTF(Url);
                stream.Write(Status);
                if (Header != null)
                {
                    stream.Write((byte)Header.Count);
                    foreach (var item in Header)
                    {
                        stream.WriteShortUTF(item.Key);
                        stream.WriteShortUTF(item.Value);
                    }
                }
                else
                {
                    stream.Write((byte)0);
                }

                if (Data != null)
                {
                    stream.Write((byte)Data.Length);
                    for (int i = 0; i < Data.Length; i++)
                    {
                        rpcOption.ParameterFormater.Encode(rpcOption, Data[i], stream);
                    }
                }
                else
                {
                    stream.Write((byte)0);
                }

                head.Full(stream.CacheLength - postion);
            }
            catch (Exception e_)
            {
                throw new BXException($"Write protocol data error {e_.Message}", e_);
            }
        }


        public void LoadParameters<T, T1, T2, T3, T4>()
        {
            LoadParameters(typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        }

        public void LoadParameters<T, T1, T2, T3>()
        {
            LoadParameters(typeof(T), typeof(T1), typeof(T2), typeof(T3));
        }

        public void LoadParameters<T, T1, T2>()
        {
            LoadParameters(typeof(T), typeof(T1), typeof(T2));
        }

        public void LoadParameters<T, T1>()
        {
            LoadParameters(typeof(T), typeof(T1));
        }

        public void LoadParameters<T>()
        {
            LoadParameters(typeof(T));
        }

        public void LoadParameters(params Type[] types)
        {
            Options options = mOption;
            try
            {
                mBufferData = types == null || types.Length == 0;
                if (PayloadData != null)
                {
                    var buffer = PayloadData.Value.Array;
                    int offset = PayloadData.Value.Offset;
                    if (Status == (short)StatusCode.SUCCESS)
                    {
                        Data = new object[Paramters];
                        for (int i = 0; i < Paramters; i++)
                        {
                            Type type = (types == null || types.Length == 0) ? null : types[i];
                            int len = BitConverter.ToInt32(buffer, offset);
                            offset += 4;
                            Data[i] = options.ParameterFormater.Decode(
                                options, type, new ArraySegment<byte>(buffer, offset, len));
                            offset += len;
                        }
                    }
                    else
                    {
                        int len = BitConverter.ToInt32(buffer, offset);
                        offset += 4;
                        object error = options.ParameterFormater.Decode(options, typeof(string), new ArraySegment<byte>(buffer, offset, len));
                        Data = new object[] { error };
                    }
                }
            }
            finally
            {
                if (PayloadData != null)
                {
                    options.PushBuffer(PayloadData.Value.Array, ContentLength);
                    PayloadData = null;
                }
            }
        }

        private int mIsDisposed = 0;

        public void Dispose()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mIsDisposed, 1, 0) == 0)
            {
                if (mBufferData && Data != null)
                {
                    foreach (var item in Data)
                    {
                        if (item is ArraySegment<byte> buffer)
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer.Array);
                        }
                    }
                }
                if (PayloadData != null)
                {
                    mOption.PushBuffer(PayloadData.Value.Array, ContentLength);
                    PayloadData = null;
                }
            }
        }

        public void ReplyError(short status, string message)
        {
            RPCPacket response = new RPCPacket();
            response.Status = status;
            response.Data = new string[] { message };
            ReplyPacket(response);
        }
        public void ReplyError(string message)
        {
            RPCPacket response = new RPCPacket();
            response.Status = (short)StatusCode.INNER_ERROR;
            response.Data = new string[] { message };
            ReplyPacket(response);
        }

        public void ReplySuccess()
        {
            RPCPacket response = new RPCPacket();
            response.Status = (short)StatusCode.SUCCESS;
            ReplyPacket(response);
        }

        public void Reply(params object[] data)
        {
            RPCPacket response = new RPCPacket();
            response.Status = (short)StatusCode.SUCCESS;
            response.Data = data;
            ReplyPacket(response);
        }

        public void ReplyPacket(RPCPacket response)
        {
            response.ID = this.ID;
            if (Session != null)
                Session.Send(response);
            if (Client != null)
                Client.Send(response);
        }
    }
}
