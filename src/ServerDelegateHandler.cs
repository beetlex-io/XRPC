using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using BeetleX.XRPC.Clients;

namespace BeetleX.XRPC
{
    class ServerDelegateHandler : Clients.DelegateHandler
    {
        public ServerDelegateHandler(Type type) : base(type)
        {

        }

        public ISession Session { get; set; }

        public XRPCServer Server { get; set; }

        public Delegate SessionDelegateProxy { get; set; }

        protected override Task OnVoidExecute(params object[] data)
        {
            RPCPacket packet = new RPCPacket();
            packet.NeedReply = false;
            packet.Url = Clients.XRPCClient.DELEGATE_TAG + Name;
            packet.Data = data;
            Server.Send(packet, new ISession[] { Session });
            return Task.CompletedTask;
        }

        protected override async Task<T> OnTaskExecute<T>(params object[] data)
        {
            bool istask = typeof(T) == typeof(RESULT_NULL);
            RPCPacket packet = new RPCPacket();
            packet.NeedReply = true;
            packet.Url = Clients.XRPCClient.DELEGATE_TAG + Name;
            packet.Data = data;
            var returltype = GetReturnTypes();
            var result = await Server.SendWait(packet, Session, returltype);
            if (result.Status != (short)StatusCode.SUCCESS)
            {
                throw new XRPCException((string)result.Data[0]);
            }
            if (istask)
                return (T)(object)new RESULT_NULL();
            return (T)result.Data[0];
        }

        public void Bind(ISession session, XRPCServer server)
        {
            this.Session = session;
            this.Server = server;
            this.SessionDelegateProxy = CreateDelegate();
        }
    }

   

}
