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

        public long PingTime { get; set; }


        public bool TimeOut(long time)
        {
            return (TimeWatch.GetElapsedMilliseconds() - PingTime) > time;
        }

        private int mPingStatus = 0;

        public async void Ping()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mPingStatus, 1, 0) == 0)
            {
                if (TcpClient.IsConnected)
                {
                    try
                    {
                        RPCPacket request = new RPCPacket();
                        request.Url = "/__System/Ping";
                        var response = await XRPCClient.SendWait(request, TcpClient, null);
                        PingTime = TimeWatch.GetElapsedMilliseconds();
                    }
                    catch (Exception e_)
                    {

                    }
                    finally
                    {
                        if (TimeOut(XRPCClient.PingTimeout * 1000))
                        {
                            TcpClient.DisConnect();
                            bool isnew;
                            TcpClient.Connect(out isnew);
                        }
                    }
                }
                else
                {
                    PingTime = TimeWatch.GetElapsedMilliseconds();
                }
                System.Threading.Interlocked.Exchange(ref mPingStatus, 0);
            }
        }

    }
}
