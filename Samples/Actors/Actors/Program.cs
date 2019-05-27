using BeetleX.EventArgs;
using BeetleX.XRPC;
using System;

namespace Actors
{
    class Program
    {
        private static XRPCServer mXRPCServer;

        static void Main(string[] args)
        {
            mXRPCServer = new XRPCServer();
            mXRPCServer.ServerOptions.LogLevel = LogType.Error;
            mXRPCServer.Register(typeof(Program).Assembly);
            mXRPCServer.Open();
            Console.Read();
        }
    }
}
