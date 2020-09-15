using BeetleX.Clients;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC.Clients
{
    public class TcpClientItem
    {
        public TcpClientItem(AsyncTcpClient client, XRPCClient xrpc)
        {
            TcpClient = client;
            XRPCClient = xrpc;
        }

        public XRPCClient XRPCClient { get; private set; }

        public AsyncTcpClient TcpClient { get; private set; }

        public bool Connected => TcpClient.IsConnected;

        private int mPingStatus = 0;

        private int mPingError = 0;

        public async void Ping()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mPingStatus, 1, 0) == 0)
            {
                try
                {
                    RPCPacket request = new RPCPacket();
                    request.Url = "/__System/Ping";
                    var response = await XRPCClient.SendWait(request, TcpClient, null);
                }
                catch (Exception e_)
                {
                    mPingError++;
                    if (mPingError > 3)
                    {
                        mPingError = 0;
                        TcpClient.DisConnect();
                    }
                }
                System.Threading.Interlocked.Exchange(ref mPingStatus, 0);
            }
        }
    }
}
