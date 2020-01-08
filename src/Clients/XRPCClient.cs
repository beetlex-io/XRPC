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
using System.Net.Security;

namespace BeetleX.XRPC.Clients
{
    public class XRPCClient
    {
        public EventClientError NetError
        {
            get;
            set;
        }

        public XRPCClient(string host, int port, string sslServiceName, int maxConnections = 1)
        {
            mSslServerName = sslServiceName ?? Host;
            Host = host;
            Port = port;
            MaxConnections = maxConnections;
            Init();
        }

        public XRPCClient(string host, int port, int maxConnections = 1)
        {
            Host = host;
            Port = port;
            MaxConnections = maxConnections;
            Init();
        }

        private void Init()
        {
            mReceiveDispatchCenter = new DispatchCenter<RPCPacket>(OnProcess);
            mAwaiterFactory = new Awaiter.AwaiterFactory(Awaiter.AwaiterFactory.CLIENT_START, Awaiter.AwaiterFactory.CLIENT_END);
            InitConnect();
            mPingTimer = new System.Threading.Timer(OnPing, null, 10000, 10000);
        }

        private List<TcpClientItem> mClients = new List<TcpClientItem>();

        public ControllerCenter Controllers { get; private set; } = new ControllerCenter();

        public List<TcpClientItem> Clients => mClients;

        private RemoteCertificateValidationCallback mCertificateValidationCallback;

        public RemoteCertificateValidationCallback CertificateValidationCallback
        {
            get
            {
                return mCertificateValidationCallback;
            }
            set
            {
                mCertificateValidationCallback = value;
                foreach (var item in mClients)
                    item.TcpClient.CertificateValidationCallback = mCertificateValidationCallback;
            }
        }

        public int MaxConnections { get; private set; }

        public Options Options { get; set; } = new Options();

        private long mRequests = 0;

        private long mResponses = 0;

        private long mIndex;

        private string mSslServerName;

        private DispatchCenter<RPCPacket> mReceiveDispatchCenter;

        private Awaiter.AwaiterFactory mAwaiterFactory;

        public Awaiter.AwaiterFactory AwaiterFactory => mAwaiterFactory;

        public System.EventHandler<RPCPacket> Receive { get; set; }

        private async void InvokeController(ControllerCenter.HandlerItem handler, RPCPacket packet)
        {
            RPCPacket response = new RPCPacket();
            try
            {
                packet.LoadParameters(handler.Parameters);
                var result = handler.Handler.Execute(handler.Controller, packet.Data);
                if (result is Task task)
                    await task;
                var data = handler.GetValue(result);
                if (data != null)
                    response.Data = new object[] { data };
            }
            catch (Exception e_)
            {
                response.Status = (short)StatusCode.INNER_ERROR;
                response.Data = new string[] { e_.Message };
            }
            packet.ReplyPacket(response);
        }

        protected virtual void OnProcess(RPCPacket response)
        {
            var awaitItem = mAwaiterFactory.GetItem(response.ID);
            if (awaitItem != null)
            {
                response.ResultType = awaitItem.ResultType;
                try
                {
                    response.LoadParameters(response.ResultType);
                }
                catch (Exception e_)
                {
                    response.Status = (short)StatusCode.INNER_ERROR;
                    response.Data = new object[] { $"{e_.Message}@{e_.StackTrace}" };
                }

                mAwaiterFactory.Completed(awaitItem, response);
            }
            else
            {
                try
                {
                    //notfound;
                    var item = Controllers.GetHandler(response.Url);
                    if (item != null)
                        InvokeController(item, response);
                    else
                    {
                        if (Receive != null)
                            Receive(this, response);
                        else
                        {
                            if (response.NeedReply)
                            {
                                var result = new RPCPacket();
                                result.Status = (short)StatusCode.NOT_SUPPORT;
                                result.Data = new object[] { $"{response.Url} not found!" };
                                response.ReplyPacket(result);
                            }
                        }
                    }
                }
                catch (Exception e_)
                {
                    OnError(response.Client, new ClientErrorArgs { Error = e_, Message = $"Packet process event error {e_.Message}" });
                }
            }
        }

        private void OnError(IClient client, ClientErrorArgs e)
        {
            try
            {
                NetError.Invoke(client, e);
            }
            catch
            {

            }

        }

        private void OnPacketCompleted(IClient client, object message)
        {
            RPCPacket response = (RPCPacket)message;
            System.Threading.Interlocked.Increment(ref mResponses);
            mReceiveDispatchCenter.Enqueue(response);
        }

        private void InitConnect()
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
                client.ClientError = OnError;
                bool isnew;
                client.Connect(out isnew);
                mClients.Add(new TcpClientItem(client, this));
            }
        }

        private AsyncTcpClient GetClient()
        {
            long index = System.Threading.Interlocked.Increment(ref mIndex);
            return mClients[(int)(index % mClients.Count)].TcpClient;
        }

        public long Requests => mRequests;

        public long Responses => mResponses;

        public string Host { get; set; }

        public int Port { get; set; }

        public int TimeOut { get; set; } = 1000 * 10;

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

        public void Register<Service>(Service serviceImpl)
        {
            Controllers.Register<Service>(serviceImpl);
        }

        public void Send(RPCPacket request, AsyncTcpClient client = null)
        {
            client = client ?? GetClient();
            bool isnew;
            if (client.Connect(out isnew))
            {
                client.Send(request);
                System.Threading.Interlocked.Increment(ref mRequests);
            }
            else
            {
                throw client.LastError;
            }
        }

        public Task<RPCPacket> SendWait(RPCPacket request, AsyncTcpClient client, Type[] resultType = null)
        {
            client = client ?? GetClient();
            bool isnew;
            if (client.Connect(out isnew))
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
        #region ping

        private System.Threading.Timer mPingTimer;

        private int mPingTimeout = 0;

        public int PingTimeout
        {
            get
            {
                return mPingTimeout;
            }
            set
            {
                mPingTimeout = value;
                if (mPingTimeout > 0)
                    mPingTimer.Change(1000, 1000);
                else
                    mPingTimer.Change(100000, 100000);
            }
        }

        private void OnPing(object state)
        {
            foreach (var item in Clients)
            {
                item.Ping();
            }
        }
        #endregion

    }
}
