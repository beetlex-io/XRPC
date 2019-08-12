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

        private TaskCompletionSource<Response> completionSource;

        public int ID { get; set; }

        public double TimeOut { get; set; }

        private int mFree = 0;

        public Type[] ResultType { get; set; }

        public TaskCompletionSource<Response> Create(long expiredTime)
        {
            TimeOut = expiredTime;
            completionSource = new TaskCompletionSource<Response>();
            return completionSource;
        }

        public bool Completed(Response data)
        {
            if (System.Threading.Interlocked.CompareExchange(ref mFree, 1, 0) == 0)
            {
                data.ResultType = ResultType;
                completionSource.TrySetResult(data);
                return true;
            }
            return false;
        }

        public Request Request { get; set; }

        public Response Response { get; set; }

    }
}
