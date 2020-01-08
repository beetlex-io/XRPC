using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq;
namespace BeetleX.XRPC.Clients
{
    public class ControllerCenter
    {

        private ConcurrentDictionary<string, HandlerItem> mHandlers = new ConcurrentDictionary<string, HandlerItem>(
            StringComparer.OrdinalIgnoreCase);

        public void Register<Service>(Service serviceImpl)
        {
            Type type = typeof(Service);
            if (!type.IsInterface)
            {
                throw new XRPCException($"{type} not interface!");
            }
            if (!serviceImpl.GetType().IsClass)
            {
                throw new XRPCException($"{serviceImpl} not the implementer!");
            }
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                string url = $"/{type.Name}/{method.Name}";
                var item = new HandlerItem { Controller = serviceImpl, Url = url, Handler = new ClientActionHandler(method) };
                item.Parameters = (from a in method.GetParameters() select a.ParameterType).ToArray();
                mHandlers[url] = item;
            }
        }

        public HandlerItem GetHandler(string url)
        {
            mHandlers.TryGetValue(url, out HandlerItem result);
            return result;
        }

        public class HandlerItem
        {
            public string Url { get; internal set; }

            public object Controller { get; internal set; }

            public ClientActionHandler Handler { get; internal set; }

            public Type[] Parameters { get; internal set; }

            public object GetValue(object result)
            {
                if (Handler.IsVoid)
                    return null;
                if (Handler.IsTaskResult)
                {
                    if (Handler.ResultProperty != null)
                        return Handler.ResultPropertyInfo.GetValue(result);
                    return null;
                }
                return null;
            }
        }

    }
}
