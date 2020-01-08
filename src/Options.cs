using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
namespace BeetleX.XRPC
{
    public class Options
    {

        //private BufferPoolGroup mBufferPoolGroup = new BufferPoolGroup();

        public bool LogToConsole { get; set; } = true;

        public bool LogToFile { get; set; } = true;

        public int MessageMaxLength { get; set; } = 1024 * 1024;

        public int ParameterMaxLength { get; set; } = 1024 * 1024;

        public Packets.IParameterFormater ParameterFormater { get; set; } = new Packets.MsgPacket();

        public byte[] PopBuffer(int length)
        {
            //var pool = mBufferPoolGroup.GetPool(length);
            //if (!pool.TryDequeue(out byte[] result))
            //{
            //    result = mBufferPoolGroup.CreateBuffer(length, MessageMaxLength);
            //}
            //return result;
            return System.Buffers.ArrayPool<Byte>.Shared.Rent(length);

        }

        public void PushBuffer(byte[] buffer, int length)
        {
            //var pool = mBufferPoolGroup.GetPool(length);
            //pool.Enqueue(buffer);
            System.Buffers.ArrayPool<Byte>.Shared.Return(buffer);
        }

        //class BufferPoolGroup
        //{
        //    private ConcurrentQueue<byte[]> _4kbuffers = new ConcurrentQueue<byte[]>();

        //    private ConcurrentQueue<byte[]> _8kbuffers = new ConcurrentQueue<byte[]>();

        //    private ConcurrentQueue<byte[]> _32kbuffers = new ConcurrentQueue<byte[]>();

        //    private ConcurrentQueue<byte[]> _64kbuffers = new ConcurrentQueue<byte[]>();

        //    private ConcurrentQueue<byte[]> _128kbuffers = new ConcurrentQueue<byte[]>();

        //    private ConcurrentQueue<byte[]> _otherbuffers = new ConcurrentQueue<byte[]>();

        //    public ConcurrentQueue<byte[]> GetPool(int length)
        //    {
        //        if (length < 1024 * 4)
        //            return _4kbuffers;
        //        else if (length < 1024 * 8)
        //            return _8kbuffers;
        //        else if (length < 1024 * 32)
        //            return _32kbuffers;
        //        else if (length < 1024 * 64)
        //            return _64kbuffers;
        //        else if (length < 1024 * 128)
        //            return _128kbuffers;
        //        else
        //            return _otherbuffers;
        //    }

        //    public byte[] CreateBuffer(int length, int maxLength)
        //    {
        //        if (length < 1024 * 4)
        //            return new byte[1024 * 4];
        //        else if (length < 1024 * 8)
        //            return new byte[1024 * 8];
        //        else if (length < 1024 * 32)
        //            return new byte[1024 * 32];
        //        else if (length < 1024 * 64)
        //            return new byte[1024 * 64];
        //        else if (length < 1024 * 128)
        //            return new byte[1024 * 128];
        //        else
        //            return new byte[maxLength];
        //    }
        //}
    }



}
