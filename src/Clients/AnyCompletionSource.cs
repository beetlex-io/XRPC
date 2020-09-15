using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.XRPC.Clients
{
    interface IAnyCompletionSource
    {
        void Success(object data);
        void Error(Exception error);
        void WaitResponse(Task<RPCPacket> task);

        ClientActionHandler ClientActionHandler { get; set; }

        Task GetTask();
    }

    class AnyCompletionSource<T> : TaskCompletionSource<T>, IAnyCompletionSource
    {
        public void Success(object data)
        {
            TrySetResult((T)data);
        }

        public void Error(Exception error)
        {
            TrySetException(error);
        }

        public ClientActionHandler ClientActionHandler { get; set; }

        public async void WaitResponse(Task<RPCPacket> task)
        {
            try
            {
                var response = await task;
                if (response.Status != (short)StatusCode.SUCCESS)
                {
                    XRPCException error = new XRPCException((string)response.Data[0]);
                    Error(error);
                }
                else
                {
                    if (response.Paramters > 0)
                    {
                        object result = response.Data[0];
                        Success(result);
                    }
                    else
                    {
                        Success(new object());
                    }
                }
            }catch(Exception e_)
            {
                Error(e_);
            }
        }

        public Task GetTask()
        {
            return this.Task;
        }
    }

}
