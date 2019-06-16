using System;
using System.Threading.Tasks;
using BeetleX.XRPC;
using EventNext;
namespace HelloWorld
{

    class Program
    {
        private static XRPCServer mXRPCServer;
        static void Main(string[] args)
        {
            mXRPCServer = new XRPCServer();
            mXRPCServer.RPCOptions.LogToConsole = true;
            mXRPCServer.ServerOptions.LogLevel = BeetleX.EventArgs.LogType.Debug;
            mXRPCServer.Register(typeof(Program).Assembly);
            mXRPCServer.Open();
            Console.Read();
        }
    }
    [Service(typeof(IHelloWorld))]
    public class HelloWorldService : IHelloWorld
    {
        public Task<string> Hello(string name)
        {
            return $"Hello {name} {DateTime.Now}".ToTask();
        }
    }

    public interface IHelloWorld
    {
        Task<string> Hello(string name);
    }
}
