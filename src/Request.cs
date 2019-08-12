using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public class Request
    {

        private int mRetain;

        public int ID { get; set; }

        public string Url { get; set; }

        public Dictionary<string, string> Header
        {
            get; set;
        }

        public int Length { get; internal set; }

        public int Paramters { get; internal set; }

        public object[] Data { get; set; }

        public ArraySegment<byte> DataBuffer { get; set; }

        public BeetleX.ISession Sesion { get; internal set; }

        public int ContentLength { get; set; }

        public override string ToString()
        {
            return Url;
        }

        public bool Read(Options rpcOption, PipeStream stream)
        {
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
                        ID = stream.ReadInt32();
                        Url = stream.ReadShortUTF();
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
                            DataBuffer = new ArraySegment<byte>(data, 0, ContentLength);
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
            try
            {
                var head = stream.Allocate(4);
                var postion = stream.CacheLength;
                stream.Write(ID);
                stream.WriteShortUTF(Url);
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

    }
}
