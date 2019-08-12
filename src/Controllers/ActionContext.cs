using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.XRPC.Controllers
{
    public class ActionContext
    {

        public ActionContext(XRPCServer server, Request request, ActionHandler handler, object controller)
        {
            Request = request;
            Server = server;
            Handler = handler;
            Controller = controller;
        }

        public object Controller { get; private set; }

        public ActionHandler Handler { get; private set; }

        public XRPCServer Server
        {
            get; private set;
        }

        public Request Request
        { get; private set; }

        public async Task<object> Execute()
        {
            var result = Handler.MethodHandler.Execute(Controller, Request.Data);
            var task = result as Task;
            if (task != null)
            {
                await task;
            }
            return Handler.GetResult(result);
        }
    }
}
