using BeetleX.Buffers;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Packets
{
    public interface IParameterFormater
    {
        void Encode(Options rpcOption, object data, PipeStream stream);

        object Decode(Options rpcOption, Type type, ArraySegment<byte> data);
    }

    //public class ProtobufPaket : IParameterFormater
    //{
    //    public object Decode(Options rpcOption, Type type, ArraySegment<byte> data)
    //    {
    //        int length = data.Count;
    //        if (length == 0)
    //        {
    //            if (type.IsValueType)
    //            {
    //                return Activator.CreateInstance(type);
    //            }
    //            return null;
    //        }
    //        else
    //        {
    //            if (type == null)
    //            {
    //                var result = new byte[length];
    //                System.Buffer.BlockCopy(data.Array, data.Offset, result, 0, data.Count);
    //                return result;
    //            }
    //            else
    //            {
    //                using (System.IO.MemoryStream memory = new System.IO.MemoryStream(data.Array, data.Offset, data.Count))
    //                {
    //                    return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(memory, null, type);
    //                }
    //            }
    //        }
    //    }

    //    public void Encode(Options rpcOption, object data, PipeStream stream)
    //    {
    //        if (data == null)
    //        {
    //            stream.Write(0);
    //        }
    //        else
    //        {
    //            var head = stream.Allocate(4);
    //            var postion = stream.CacheLength;
    //            MessagePackSerializer.NonGeneric.Serialize(data.GetType(), stream, data);
    //            head.Full(stream.CacheLength - postion);
    //        }
    //    }
    //}

    public class JsonPacket : IParameterFormater
    {
        public object Decode(Options rpcOption, Type type, ArraySegment<byte> data)
        {
            int length = data.Count;
            if (length == 0)
            {
                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type);
                }
                return null;
            }
            else
            {
                if (type == null)
                {
                    var result = new byte[length];
                    System.Buffer.BlockCopy(data.Array, data.Offset, result, 0, data.Count);
                    return result;
                }
                else
                {
                    string txt = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject(txt, type);
                }
            }
        }

        public void Encode(Options rpcOption, object data, PipeStream stream)
        {
            if (data == null)
            {
                stream.Write(0);
            }
            else
            {
                var head = stream.Allocate(4);
                var postion = stream.CacheLength;
                //MessagePackSerializer.Serialize(data.GetType(), stream, data);
                string txt = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                stream.Write(txt);
                head.Full(stream.CacheLength - postion);
            }
        }
    }


    public class MsgPacket : IParameterFormater
    {

        public object Decode(Options rpcOption, Type type, ArraySegment<byte> data)
        {
            int length = data.Count;
            if (length == 0)
            {
                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type);
                }
                return null;
            }
            else
            {
                if (type == null)
                {
                    var result = new byte[length];
                    System.Buffer.BlockCopy(data.Array, data.Offset, result, 0, data.Count);
                    return result;
                }
                else
                {

                    return MessagePackSerializer.Deserialize(type, data);
                }
            }

        }

        public void Encode(Options rpcOption, object data, PipeStream stream)
        {
            if (data == null)
            {
                stream.Write(0);
            }
            else
            {
                var head = stream.Allocate(4);
                var postion = stream.CacheLength;
                MessagePackSerializer.Serialize(data.GetType(), stream, data);
                head.Full(stream.CacheLength - postion);
            }
        }
    }

}
