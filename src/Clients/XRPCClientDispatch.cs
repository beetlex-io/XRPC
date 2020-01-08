using EventNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BeetleX.XRPC.Clients
{
    public class XRPCClientDispatch : DispatchProxy, EventNext.IHeader
    {

        private Dictionary<string, ClientActionHandler> mHandlers = new Dictionary<string, ClientActionHandler>();

        private Dictionary<string, string> mHeader = new Dictionary<string, string>();

        public Type Type { get; set; }

        public Dictionary<string, ClientActionHandler> Handlers => mHandlers;

        public XRPCClient Client { get; set; }

        public string Actor { get; set; }

        public Dictionary<string, string> Header => mHeader;

        internal void InitHandlers()
        {
            Type type = Type;
            ServiceAttribute attribute = type.GetCustomAttribute<ServiceAttribute>(false);
            string url = "/" + (attribute?.Name ?? type.Name) + "/";
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (string.Compare("Equals", method.Name, true) == 0
              || string.Compare("GetHashCode", method.Name, true) == 0
              || string.Compare("GetType", method.Name, true) == 0
              || string.Compare("ToString", method.Name, true) == 0 || method.Name.IndexOf("set_") >= 0
              || method.Name.IndexOf("get_") >= 0)
                    continue;
                ActionAttribute aa = method.GetCustomAttribute<ActionAttribute>(false);
                var actionUrl = url + (aa == null ? method.Name : aa.Name);
                var handler = mHandlers.Values.FirstOrDefault(c => c.Url == actionUrl);
                if (handler != null)
                {
                    throw new XRPCException($"{type.Name}.{method.Name} action already exists, can add ActionAttribute on the method");
                }
                else
                {
                    handler = new ClientActionHandler(method);
                    handler.Url = actionUrl;
                    mHandlers[method.Name] = handler;
                }
            }
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (!mHandlers.TryGetValue(targetMethod.Name, out ClientActionHandler handler))
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
                if (!string.IsNullOrEmpty(Actor))
                {
                    request.Header = new Dictionary<string, string>();
                    request.Header[EventCenter.ACTOR_TAG] = this.Actor;
                }
                if (mHeader.Count > 0)
                {
                    if (request.Header == null)
                        request.Header = new Dictionary<string, string>();
                    foreach (var item in mHeader)
                    {
                        request.Header.Add(item.Key, item.Value);
                    }
                }
                var task = Client.SendWait(request, null, handler.ResponseType);
                if (!handler.IsTaskResult)
                {
                    if (task.Wait(Client.TimeOut))
                    {
                        var response = task.Result;
                        if (response.Status == (short)StatusCode.SUCCESS)
                        {
                            if (response.Paramters > 0)
                                return response.Data[0];
                            return null;
                        }
                        else
                        {
                            Client.AwaiterFactory.GetItem(request.ID);
                            var error = new XRPCException((string)response.Data[0]);
                            error.ErrorCode = response.Status;
                            throw error;
                        }
                    }
                    else
                    {
                        var error = new XRPCException($"{targetMethod.Name} action time out!");
                        error.ErrorCode = (short)StatusCode.REQUEST_TIMEOUT;
                        throw error;
                    }
                }
                else
                {
                    IAnyCompletionSource source = handler.GetCompletionSource();
                    source.WaitResponse(task);
                    return source.GetTask();
                }

            }
        }
    }
}
