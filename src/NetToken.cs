using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public class NetToken
    {
        public NetToken()
        {

        }

        public ISession Session { get; internal set; }

        public IServer Server { get; internal set; }

        public Request Request { get; internal set; }

        public override string ToString()
        {
            return $"{Request.Sesion.RemoteEndPoint}";
        }
    }
}
