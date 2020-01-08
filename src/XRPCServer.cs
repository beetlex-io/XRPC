using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BeetleX.XRPC.Clients;

namespace BeetleX.XRPC
{
    public class XRPCServer : ServerHandlerBase, IDisposable
    {
        public XRPCServer()
        {
            mFileLog = new FileLogWriter("BEETLEX_XRPC_SERVER");

            mRequestDispatchCenter = new Dispatchs.DispatchCenter<RPCPacket>(OnRequestProcess);
            this.ServerOptions.BufferSize = 1024 * 8;
        }

        private long mRequests = 0;

        private long mResponses = 0;

        private BeetleX.IServer mServer;

        private FileLogWriter mFileLog;

        private BeetleX.Dispatchs.DispatchCenter<RPCPacket> mRequestDispatchCenter;

        private void BindRequestParameter(RPCPacket request, EventNext.EventActionHandler handler)
        {
            request.LoadParameters(handler != null ? handler.ParametersType : null);
        }

        private Dictionary<string, object> mProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private Awaiter.AwaiterFactory AwaiterFactory = new Awaiter.AwaiterFactory(Awaiter.AwaiterFactory.SERVER_START, Awaiter.AwaiterFactory.SERVER_END);

        class EventCompleted : EventNext.IEventCompleted
        {

            public XRPCServer Server { get; set; }

            public RPCPacket Request { get; set; }

            public void Completed(EventNext.IEventOutput data)
            {
                RPCPacket response = new RPCPacket();
                response.Status = (short)data.EventError;
                response.Header = data.Properties;
                response.Data = data.Data;
                data.Token = null;
                if (Server.EnableLog(LogType.Debug))
                {
                    string info = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    Server.Log(LogType.Debug, $"[{data.ID}]{Request.Sesion.RemoteEndPoint} send event data:{info}");
                }
                Server.OnResponse(Request, response);
            }
        }

        private void OnEventNext(RPCPacket e)
        {
            EventNext.EventInput input = new EventNext.EventInput();
            input.ID = e.ID;
            input.EventPath = e.Url;
            input.Properties = e.Header;
            input.Data = e.Data;
            if (EnableLog(LogType.Debug))
            {
                string info = Newtonsoft.Json.JsonConvert.SerializeObject(input);
                Log(LogType.Debug, $"[{input.ID}]{e.Sesion.RemoteEndPoint} receive event data:{info}");
            }
            input.Token = new XRPCEventToken { Request = e, Server = this, Session = e.Sesion };
            EventCenter.Execute(input, new EventCompleted { Server = this, Request = e });

        }

        private void OnRequestProcess(RPCPacket e)
        {
            RPCPacket response = e;
            try
            {
                EventNext.EventActionHandler handler = EventCenter.GetActionHandler(e.Url);
                if (handler != null)
                {
                    BindRequestParameter(e, handler);
                    OnEventNext(e);
                }
                else
                {
                    if (e.Url == "/__System/Ping")
                    {
                        response = new RPCPacket();
                        if (EnableLog(LogType.Debug))
                        {
                            Log(LogType.Debug, $"[{e.ID}]{e.Sesion.RemoteEndPoint} request {e.Url}");
                        }
                        response.Status = (short)StatusCode.SUCCESS;
                        OnResponse(e, response);
                    }
                    else
                    {
                        var awaitItem = AwaiterFactory.GetItem(response.ID);
                        if (awaitItem != null)
                        {
                            response.ResultType = awaitItem.ResultType;
                            try
                            {
                                if (response.ResultType != null)
                                {
                                    response.LoadParameters(response.ResultType);
                                }
                            }
                            catch (Exception e_)
                            {
                                response.Status = (short)StatusCode.INNER_ERROR;
                                response.Data = new object[] { $"{e_.Message}@{e_.StackTrace}" };
                            }
                            AwaiterFactory.Completed(awaitItem, response);
                        }
                        else
                        {

                            if (Receive == null)
                            {
                                if (EnableLog(LogType.Debug))
                                {
                                    Log(LogType.Debug, $"[{e.ID}]{e.Sesion.RemoteEndPoint} request {e.Url} not found!");
                                }
                                response = new RPCPacket();
                                response.Status = (short)StatusCode.ACTION_NOT_FOUND;
                                response.Data = new object[] { $"request {e.Url} not found!" };
                                OnResponse(e, response);
                            }
                            else
                            {
                                Receive.Invoke(this, new Events.EventActionNotFoundArgs(this, e));
                            }
                        }
                    }

                }
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                {
                    Log(LogType.Debug, $"[{e.ID}]{e.Sesion.RemoteEndPoint} request {e.Url} error {e_.Message}@{e_.StackTrace}!");
                }
            }
        }

        public int ClientTimeout { get; set; } = 1000 * 10;

        public event EventHandler<Events.EventActionNotFoundArgs> Receive;

        public void Send(RPCPacket response, ISession[] sessions)
        {
            if (sessions != null)
            {
                foreach (var item in sessions)
                {
                    System.Threading.Interlocked.Add(ref mResponses, sessions.Length);
                    mServer.Send(response, sessions);
                }
            }
        }

        public void Reply(RPCPacket response, RPCPacket request)
        {
            OnResponse(request, response);
        }

        public Task<RPCPacket> SendWait(RPCPacket request, ISession session, int timeout = 10000)
        {
            return SendWait(request, session, null, timeout);
        }

        internal Task<RPCPacket> SendWait(RPCPacket request, ISession session, Type[] resultTypes, int timeout = 10000)
        {
            var result = AwaiterFactory.Create(request, resultTypes, timeout);
            request.ID = result.Item1;
            session.Send(request);
            System.Threading.Interlocked.Increment(ref mRequests);
            return result.Item2.Task;
        }

        public long Requests => mRequests;

        public long Responses => mResponses;

        public void Register(object controller)
        {

            EventCenter.Register(controller);
        }

        public void Register(params System.Reflection.Assembly[] assemblies)
        {
            EventCenter.Register(assemblies);
        }

        public BeetleX.ServerOptions ServerOptions { get; private set; } = new ServerOptions();

        public Options RPCOptions { get; private set; } = new Options();

        public IServer Server => mServer;

        public event System.EventHandler<ConnectedEventArgs> RPCConnected;

        public event System.EventHandler<SessionEventArgs> RPCDisconnect;

        public void Log(LogType type, string message, params object[] parameters)
        {
            Log(type, string.Format(message, parameters));
        }

        public EventHandler<BeetleX.EventArgs.ServerLogEventArgs> Logger;

        private EventNext.EventCenter mEventCenter = new EventNext.EventCenter();

        private int mEventCenterInit = 0;

        public EventNext.EventCenter EventCenter
        {
            get
            {
                if (System.Threading.Interlocked.CompareExchange(ref mEventCenterInit, 1, 0) == 0)
                {
                    mEventCenter.LogLevel = (EventNext.LogType)ServerOptions.LogLevel;
                    mEventCenter.LogOutput += OnEventLog;
                }
                return mEventCenter;
            }
        }

        public override void Log(IServer server, ServerLogEventArgs e)
        {
            if (Logger == null)
            {
                if (RPCOptions.LogToConsole)
                    base.Log(server, e);
                if (RPCOptions.LogToFile)
                    mFileLog.Add(e.Type, e.Message);
            }
            else
                Logger(server, e);
        }

        public bool EnableLog(LogType logType)
        {
            return (int)(ServerOptions.LogLevel) <= (int)logType;
        }

        public void Log(LogType type, string message)
        {
            try
            {
                Log(null, new ServerLogEventArgs(message, type));
            }
            catch { }
        }

        public override void Connecting(IServer server, ConnectingEventArgs e)
        {
            if (server.Count > ServerOptions.MaxConnections)
            {
                e.Cancel = true;
                if (EnableLog(LogType.Warring))
                    Log(LogType.Warring, $"XRPC Maximum online limit,{e.Socket.RemoteEndPoint} closed!");
            }
        }

        public override void Connected(IServer server, ConnectedEventArgs e)
        {
            base.Connected(server, e);
            RPCConnected?.Invoke(this, e);
        }

        public override void Disconnect(IServer server, SessionEventArgs e)
        {
            base.Disconnect(server, e);
            RPCDisconnect?.Invoke(this, e);
        }

        protected override void OnReceiveMessage(IServer server, ISession session, object message)
        {
            base.OnReceiveMessage(server, session, message);
            RPCPacket request = (RPCPacket)message;
            if (EnableLog(LogType.Debug))
            {
                Log(LogType.Debug, $"[{request.ID}]{session.RemoteEndPoint} receive message {request.Url}@{request.ID}");
            }
            System.Threading.Interlocked.Increment(ref mRequests);
            Server.UpdateSession(session);
            mRequestDispatchCenter.Enqueue(request);
        }

        public void Open()
        {
            BeetleX.XRPC.Packets.ServerPacket serverPacke = new Packets.ServerPacket();
            serverPacke.Options = this.RPCOptions;
            mServer = SocketFactory.CreateTcpServer(this, serverPacke, ServerOptions);
            mServer.WriteLogo = OutputLogo;
            mServer.Open();

        }

        private void OutputLogo()
        {
            AssemblyCopyrightAttribute productAttr = typeof(BeetleX.BXException).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            var logo = "\r\n";
            logo += "*******************************************************************************\r\n";
            logo += " BeetleX xrpc service framework \r\n";

            logo += $" {productAttr.Copyright}\r\n";
            logo += $" ServerGC    [{GCSettings.IsServerGC}]\r\n";
            logo += $" BeetleX     Version [{typeof(BeetleX.BXException).Assembly.GetName().Version}]\r\n";
            logo += $" XRPC        Version [{ typeof(XRPCServer).Assembly.GetName().Version}] \r\n";
            logo += " -----------------------------------------------------------------------------\r\n";
            foreach (var item in Server.Options.Listens)
            {
                logo += $" {item}\r\n";
            }
            logo += "*******************************************************************************\r\n";

            Server.Log(LogType.Info, null, logo);
        }

        private void OnEventLog(object sender, EventNext.Events.EventLogArgs e)
        {
            Log((LogType)e.Type, e.Message);
        }

        internal void OnResponse(RPCPacket request, RPCPacket response)
        {
            System.Threading.Interlocked.Increment(ref mResponses);
            response.ID = request.ID;
            request.Sesion.Send(response);
        }

        public object this[string name]
        {
            get
            {
                mProperties.TryGetValue(name, out object result);
                return result;
            }
            set
            {
                mProperties[name] = value;
            }
        }

        public void Dispose()
        {
            if (mServer != null)
            {
                mServer.Dispose();
                mServer = null;
            }
        }

        public static XRPCEventToken EventToken
        {
            get
            {
                return (XRPCEventToken)EventNext.EventCenter.EventActionContext?.Input.Token;
            }
        }

        public T GetClient<T>(ISession session)
        {
            string key = typeof(T).Name;
            object result = session[key];
            if (result == null)
            {
                result = DispatchProxy.Create<T, XRPCSeverInvokeClientDispatch>();
                XRPCSeverInvokeClientDispatch dispatch = ((XRPCSeverInvokeClientDispatch)result);
                dispatch.Session = session;
                dispatch.Server = this;
                dispatch.Type = typeof(T);
                dispatch.InitHandlers();
                return (T)result;
            }
            return (T)result;

        }
    }
}
