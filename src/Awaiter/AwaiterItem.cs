using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.XRPC.Awaiter
{
    public sealed class AwaiterItem
    {
        public AwaiterItem()
        {

        }

        private TaskCompletionSource<RPCPacket> completionSource;

        public int ID { get; set; }

        public double TimeOut { get; set; }

        private int mFree = 0;

        public Type[] ResultType { get; set; }

        public TaskCompletionSource<RPCPacket> Create(long expiredTime)
        {
            TimeOut = expiredTime;
            completionSource = new TaskCompletionSource<RPCPacket>();
            return completionSource;
        }

        public bool Completed(RPCPacket data)
        {
            if (System.Threading.Interlocked.CompareExchange(ref mFree, 1, 0) == 0)
            {
                data.ResultType = ResultType;
                completionSource.TrySetResult(data);
                return true;
            }
            return false;
        }

        public RPCPacket Request { get; set; }

        public RPCPacket Response { get; set; }

    }
}
