using BeetleX.Clients;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using BeetleX.Dispatchs;
using EventNext;

namespace BeetleX.XRPC.Clients
{
    public class XRPCClient
    {
        static XRPCClient()
        {
            BeetleX.Buffers.BufferPool.BUFFER_SIZE = 1024 * 32;
        }

        public EventClientError NetError
        {
            get;
            set;
        }

        public XRPCClient(string host, int port, int maxConnections = 2)
        {
            Host = host;
            Port = port;
            mReceiveDispatchCenter = new DispatchCenter<Response>(OnProcess);
            MaxConnections = maxConnections;
            mAwaiterFactory = new Awaiter.AwaiterFactory();
        }

        private List<AsyncTcpClient> mClients = new List<AsyncTcpClient>();

        public int MaxConnections { get; private set; }

        public Options Options { get; set; } = new Options();

        private long mRequests = 0;

        private long mResponses = 0;

        private long mIndex;

        private string mSslServerName;

        private DispatchCenter<Response> mReceiveDispatchCenter;

        private Awaiter.AwaiterFactory mAwaiterFactory;

        public void SslEnabled(string serverName = null)
        {
            mSslServerName = serverName ?? Host;
        }

        private void OnProcess(Response response)
        {
            var awaitItem = mAwaiterFactory.GetItem(response.ID);
            if (awaitItem != null)
            {
                response.ResultType = awaitItem.ResultType;
                try
                {
                    if (response.Status == (short)ResponseCode.SUCCESS)
                    {
                        response.Data = new Object[response.Paramters];
                        var results = response.ResultType;
                        var buffer = response.DataBuffer.Array;
                        int offset = response.DataBuffer.Offset;
                        for (int i = 0; i < response.Paramters; i++)
                        {
                            int len = BitConverter.ToInt32(buffer, offset);
                            offset += 4;
                            response.Data[i] = Options.ParameterFormater.Decode(
                                Options, results != null && i < results.Length ?
                                response.ResultType[i] : null, new ArraySegment<byte>(buffer, offset, len));
                            offset += len;
                        }
                    }
                    else
                    {
                        var buffer = response.DataBuffer.Array;
                        int offset = response.DataBuffer.Offset;
                        int len = BitConverter.ToInt32(buffer, offset);
                        offset += 4;
                        object error = Options.ParameterFormater.Decode(Options, typeof(string), new ArraySegment<byte>(buffer, offset, len));
                        response.Data = new object[] { error };
                    }
                }
                catch (Exception e_)
                {
                    response.Status = (short)ResponseCode.INNER_ERROR;
                    response.Data = new object[] { $"{e_.Message}@{e_.StackTrace}" };
                }
                finally
                {
                    var array = response.DataBuffer;
                    if (array != null)
                    {
                        this.Options.PushBuffer(array.Array, array.Count);
                    }
                    response.DataBuffer = null;
                }

                mAwaiterFactory.Completed(awaitItem, response);
            }
            else
            {
                //notfound;
                OnNotFound(response);
            }
        }

        private void OnNotFound(Response response)
        {
            try
            {
                if (response.Status == (short)ResponseCode.SUCCESS)
                {
                    response.Data = new Object[response.Paramters];
                    var buffer = response.DataBuffer.Array;
                    int offset = response.DataBuffer.Offset;
                    for (int i = 0; i < response.Paramters; i++)
                    {
                        int len = BitConverter.ToInt32(buffer, offset);
                        offset += 4;
                        response.Data[i] = Options.ParameterFormater.Decode(
                            Options, null, new ArraySegment<byte>(buffer, offset, len));
                        offset += len;
                    }
                }
                else
                {
                    var buffer = response.DataBuffer.Array;
                    int offset = response.DataBuffer.Offset;
                    int len = BitConverter.ToInt32(buffer, offset);
                    offset += 4;
                    object error = Options.ParameterFormater.Decode(Options, typeof(string), new ArraySegment<byte>(buffer, offset, len));
                    response.Data = new object[] { error };
                }
            }
            catch (Exception e_)
            {
                response.Status = (short)ResponseCode.INNER_ERROR;
                response.Data = new object[] { $"{e_.Message}@{e_.StackTrace}" };
            }
            finally
            {
                var array = response.DataBuffer;
                if (array != null)
                {
                    this.Options.PushBuffer(array.Array, array.Count);
                }
                response.DataBuffer = null;
            }
        }

        private void OnPacketCompleted(IClient client, object message)
        {
            Response response = (Response)message;
            System.Threading.Interlocked.Increment(ref mResponses);
            mReceiveDispatchCenter.Enqueue(response);
        }

        public void Connect()
        {
            for (int i = 0; i < MaxConnections; i++)
            {
                var packet = new Packets.ClientPacket();
                packet.Options = Options;
                AsyncTcpClient client;
                if (mSslServerName == null)
                {
                    client = BeetleX.SocketFactory.CreateClient<BeetleX.Clients.AsyncTcpClient>(

                packet, Host, Port);
                }
                else
                {
                    client = BeetleX.SocketFactory.CreateSslClient<BeetleX.Clients.AsyncTcpClient>(

             packet, Host, Port, mSslServerName);
                }
                client.PacketReceive = OnPacketCompleted;
                client.ClientError = (c, e) =>
                {
                    NetError?.Invoke(c, e);
                };
                mClients.Add(client);
            }
        }

        private AsyncTcpClient GetClient()
        {
            long index = System.Threading.Interlocked.Increment(ref mIndex);
            return mClients[(int)(index % mClients.Count)];
        }

        public long Requests => mRequests;

        public long Responses => mResponses;

        public string Host { get; set; }

        public int Port { get; set; }

        public int TimeOut { get; set; } = 1000 * 100;

        public T Create<T>(string actorID = null)
        {
            object result = DispatchProxy.Create<T, XRPCClientDispatch>();
            XRPCClientDispatch dispatch = ((XRPCClientDispatch)result);
            dispatch.Client = this;
            dispatch.Actor = actorID;
            dispatch.Type = typeof(T);
            dispatch.InitHandlers();
            return (T)result;
        }

        public Task<Response> Send(Request request, Type[] resultType = null)
        {
            var client = GetClient();
            if (client.Connect())
            {
                var result = mAwaiterFactory.Create(request, resultType, TimeOut);
                request.ID = result.Item1;
                client.Send(request);
                System.Threading.Interlocked.Increment(ref mRequests);
                return result.Item2.Task;
            }
            else
            {
                throw client.LastError;
            }
        }

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
                    error.ErrorCode = (short)ResponseCode.ACTION_NOT_FOUND;
                    throw error;
                }
                else
                {
                    if (!handler.IsTaskResult)
                    {
                        var error = new XRPCException("Definition is not supported, please define task with return value!");
                        error.ErrorCode = (short)ResponseCode.NOT_SUPPORT;
                        throw error;
                    }

                    var request = new Request();
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
                    var task = Client.Send(request, handler.ResponseType);
                    if (!handler.IsTaskResult)
                    {
                        if (task.Wait(Client.TimeOut))
                        {
                            var response = task.Result;
                            if (response.Status == (short)ResponseCode.SUCCESS)
                            {
                                if (response.Paramters > 0)
                                    return response.Data[0];
                                return null;
                            }
                            else
                            {
                                Client.mAwaiterFactory.GetItem(request.ID);
                                var error = new XRPCException((string)response.Data[0]);
                                error.ErrorCode = response.Status;
                                throw error;
                            }
                        }
                        else
                        {
                            var error = new XRPCException($"{targetMethod.Name} action time out!");
                            error.ErrorCode = (short)ResponseCode.REQUEST_TIMEOUT;
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
}
