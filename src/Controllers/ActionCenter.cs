using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace BeetleX.XRPC.Controllers
{
    public class ActionCenter
    {

        public ActionCenter(XRPCServer server)
        {
            Server = server;
        }

        private ConcurrentDictionary<string, ActionHandler> mActionHadlers = new ConcurrentDictionary<string, ActionHandler>();

        protected virtual object CreateController(Type type)
        {
            if (ControllerInstance != null)
            {
                Events.EventControllerInstanceArgs e = new Events.EventControllerInstanceArgs(Server, type);
                ControllerInstance(this, e);
                return e.Controller ?? Activator.CreateInstance(type);
            }
            return Activator.CreateInstance(type);
        }

        private void OnRegister(ControllerAttribute attribute, Type type, object controller)
        {
            foreach (Type itype in attribute.Types)
            {
                if (!itype.IsInterface)
                {
                    continue;
                }
                if (type.GetInterface(itype.Name) == null)
                {
                    continue;
                }
                string url = "/" + (attribute.Name ?? itype.Name) + "/";
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    try
                    {
                        if (string.Compare("Equals", method.Name, true) == 0
                      || string.Compare("GetHashCode", method.Name, true) == 0
                      || string.Compare("GetType", method.Name, true) == 0
                      || string.Compare("ToString", method.Name, true) == 0 || method.Name.IndexOf("set_") >= 0
                      || method.Name.IndexOf("get_") >= 0)
                            continue;
                        ActionAttribute aa = method.GetCustomAttribute<ActionAttribute>(false);
                        var actionUrl = url + (aa == null ? method.Name : aa.Name);
                        if (mActionHadlers.TryGetValue(actionUrl, out ActionHandler handler))
                        {
                            Server.Log(EventArgs.LogType.Warring, $"{itype.Name}->{type.Name}.{method.Name} action already exists, can add ActionAttribute on the method");
                        }
                        else
                        {
                            handler = new ActionHandler(type, method, controller);
                            handler.SingleInstance = attribute.SingleInstance;
                            handler.Interface = itype;
                            mActionHadlers[actionUrl] = handler;

                            Server.Log(EventArgs.LogType.Info, $"Register {itype.Name}->{type.Name}@{method.Name} to {actionUrl}");
                        }
                    }
                    catch (Exception e_)
                    {
                        Server.Log(EventArgs.LogType.Error, $"Register {itype.Name}->{type.Name}@{method.Name} action error {e_.Message}@{e_.StackTrace}");
                    }
                }
            }
        }

        public XRPCServer Server { get; private set; }

        public event EventHandler<Events.EventControllerInstanceArgs> ControllerInstance;

        public void Register(object controller)
        {

            Type type = controller.GetType();
            var attribute = type.GetCustomAttribute<ControllerAttribute>(false);
            if (attribute != null)
            {
                OnRegister(attribute, type, controller);
            }
            else
            {
                Server.Log(EventArgs.LogType.Warring, $"{type.Name} no controller attribute");
            }
        }

        public void Register(params Assembly[] assemblies)
        {
            foreach (Assembly item in assemblies)
            {
                foreach (Type type in item.GetTypes())
                {
                    try
                    {
                        if (type.IsPublic && !type.IsAbstract && type.IsClass)
                        {
                            var attribute = type.GetCustomAttribute<ControllerAttribute>(false);
                            if (attribute != null)
                            {
                                object controller = CreateController(type);
                                OnRegister(attribute, type, controller);
                            }
                        }
                    }
                    catch (Exception e_)
                    {
                        Server.Log(EventArgs.LogType.Error, $"Register {type.Name} error {e_.Message}@{e_.StackTrace}");
                    }
                }
            }
        }

        public ActionHandler GetActionHandler(string url)
        {
            mActionHadlers.TryGetValue(url, out ActionHandler handler);
            return handler;
        }

        internal async void TaskExecute(Request request)
        {
            var result = await Execute(request);
            Server.OnResponse(request, result);
        } 

        public async Task<Response> Execute(Request request)
        {
            long runTime = TimeWatch.GetElapsedMilliseconds();
            Response response = new Response();
            try
            {
                response.Status = (short)ResponseCode.SUCCESS;
                ActionHandler handler = GetActionHandler(request.Url);
                if (handler == null)
                {
                    response.Status = (int)ResponseCode.ACTION_NOT_FOUND;
                    response.Data = new object[] { $"{request.Sesion?.RemoteEndPoint} execute {request.Url} not found!" };
                }
                else
                {
                    var controller = handler.Controller;
                    if (!handler.SingleInstance)
                    {
                        controller = CreateController(handler.ControllerType);
                    }
                    ActionContext context = new ActionContext(this.Server, request, handler, controller);
                    var result = await context.Execute();
                    if (result != null)
                        response.Data = new object[] { result };
                }
            }
            catch (Exception e_)
            {
                response.Status = (int)ResponseCode.INNER_ERROR;
                response.Data = new object[] { $"{request.Sesion?.RemoteEndPoint} execute {request.Url} error {e_.Message}" };
                if (Server.EnableLog(EventArgs.LogType.Error))
                    Server.Log(EventArgs.LogType.Error, $"{request.Sesion?.RemoteEndPoint} execute {request.Url} error {e_.Message}@{e_.StackTrace}");
            }
            response.ExecuteTime = TimeWatch.GetElapsedMilliseconds() - runTime;
            return response;
        }
    }
}
