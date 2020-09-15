using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BeetleX.XRPC.Clients
{
   public class XRPCSeverInvokeClientDispatch : XRPCClientDispatch
    {

        public ISession Session { get; set; }

        public XRPCServer Server { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (!Handlers.TryGetValue(targetMethod.Name, out ClientActionHandler handler))
            {
                var error = new XRPCException($"{targetMethod.Name} action not found!");
                error.ErrorCode = (short)StatusCode.ACTION_NOT_FOUND;
                throw error;
            }
            else
            {
                if (!handler.IsTaskResult)
                {
                    var error = new XRPCException("Definition is not supported, please define task with return value!");
                    error.ErrorCode = (short)StatusCode.NOT_SUPPORT;
                    throw error;
                }

                var request = new RPCPacket();
                request.Url = handler.Url;
                request.Data = args;
                var task = Server.SendWait(request, Session, handler.ResponseType);
                IAnyCompletionSource source = handler.GetCompletionSource();
                source.WaitResponse(task);
                return source.GetTask();
            }
        }
    }
}
