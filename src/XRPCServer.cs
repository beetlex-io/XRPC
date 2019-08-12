using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public class XRPCServer : ServerHandlerBase, IDisposable
    {
        public XRPCServer()
        {
            mFileLog = new FileLogWriter("BEETLEX_XRPC_SERVER");

            mRequestDispatchCenter = new Dispatchs.DispatchCenter<Request>(OnRequestProcess);
            this.ServerOptions.BufferSize = 1024 * 32;
        }

        private long mRequests = 0;

        private long mResponses = 0;

        private BeetleX.IServer mServer;

        private FileLogWriter mFileLog;

        private BeetleX.Dispatchs.DispatchCenter<Request> mRequestDispatchCenter;

        private void BindRequestParameter(Request request, EventNext.EventActionHandler handler)
        {
            request.Data = new object[request.Paramters];
            var buffer = request.DataBuffer.Array;
            int offset = request.DataBuffer.Offset;
            for (int i = 0; i < request.Paramters; i++)
            {
                int len = BitConverter.ToInt32(buffer, offset);
                offset += 4;
                request.Data[i] = RPCOptions.ParameterFormater.Decode(
                    RPCOptions, handler?.Parameters[i].Type, new ArraySegment<byte>(buffer, offset, len));
                offset += len;
            }
        }

        class EventCompleted : EventNext.IEventCompleted
        {

            public XRPCServer Server { get; set; }

            public Request Request { get; set; }

            public void Completed(EventNext.IEventOutput data)
            {
                Response response = new Response();
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


        private void OnEventNext(Request e)
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
            input.Token = new NetToken { Request = e, Server = e.Sesion.Server, Session = e.Sesion };
            EventCenter.Execute(input, new EventCompleted { Server = this, Request = e });

        }

        private void OnRequestProcess(Request e)
        {
            Response response;
            try
            {
                EventNext.EventActionHandler handler = EventCenter.GetActionHandler(e.Url);
                BindRequestParameter(e, handler);
                if (handler != null)
                {
                    OnEventNext(e);
                }
                else
                {
                    if (EnableLog(LogType.Debug))
                    {
                        Log(LogType.Debug, $"[{e.ID}]{e.Sesion.RemoteEndPoint} request {e.Url} not found!");
                    }
                    response = new Response();
                    response.Status = (short)ResponseCode.ACTION_NOT_FOUND;
                    response.Data = new object[] { $"request {e.Url} not found!" };
                    OnResponse(e, response);
                }
            }
            catch (Exception e_)
            {
                response = new Response();
                response.Status = (short)ResponseCode.INNER_ERROR;
                response.Data = new object[] { e_.Message };
                OnResponse(e, response);
                if (EnableLog(LogType.Error))
                {
                    Log(LogType.Debug, $"[{e.ID}]{e.Sesion.RemoteEndPoint} request {e.Url} error {e_.Message}@{e_.StackTrace}!");
                }
            }
            finally
            {
                var array = e.DataBuffer;
                if (array != null)
                {
                    this.RPCOptions.PushBuffer(array.Array, array.Count);
                }
                e.DataBuffer = null;
            }
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
            }
            e.Socket.NoDelay = true;
        }

        public override void Connected(IServer server, ConnectedEventArgs e)
        {
            base.Connected(server, e);
        }

        public override void Disconnect(IServer server, SessionEventArgs e)
        {
            base.Disconnect(server, e);
        }

        protected override void OnReceiveMessage(IServer server, ISession session, object message)
        {
            base.OnReceiveMessage(server, session, message);
            Request request = (Request)message;
            if (EnableLog(LogType.Debug))
            {
                Log(LogType.Debug, $"[{request.ID}]{session.RemoteEndPoint} receive message {request.Url}@{request.ID}");
            }
            System.Threading.Interlocked.Increment(ref mRequests);
            mRequestDispatchCenter.Enqueue(request);
        }

        public void Open()
        {
            BeetleX.XRPC.Packets.ServerPacket serverPacke = new Packets.ServerPacket();
            serverPacke.Options = this.RPCOptions;
            mServer = SocketFactory.CreateTcpServer(this, serverPacke, ServerOptions);

            mServer.Open();
            Log(LogType.Info, $"BeetleX XRPC started [{GetType().Assembly.GetName().Version}]");
        }

        private void OnEventLog(object sender, EventNext.Events.EventLogArgs e)
        {
            Log((LogType)e.Type, e.Message);
        }

        internal void OnResponse(Request request, Response response)
        {
            System.Threading.Interlocked.Increment(ref mResponses);
            response.ID = request.ID;
            request.Sesion.Send(response);
        }

        public void Dispose()
        {
            if (mServer != null)
            {
                mServer.Dispose();
                mServer = null;
            }
        }
    }
}
