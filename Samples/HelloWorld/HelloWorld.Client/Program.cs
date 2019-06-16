using BeetleX.XRPC.Clients;
using System;
using System.Threading.Tasks;

namespace HelloWorld.Client
{
    class Program
    {
        static XRPCClient client;
        static void Main(string[] args)
        {
            client = new XRPCClient("127.0.0.1", 9090);
            client.Connect();
            client.NetError = (c, e) =>
            {
                Console.WriteLine(e.Error.Message);
            };
            client.TimeOut = 10000;
            Hello();
            System.Threading.Thread.Sleep(-1);
        }

        static async void Hello()
        {
            var service = client.Create<IHelloWorld>();
            while(true)
            {
                Console.Write("enter name:");
                string name = Console.ReadLine();
                var result = await service.Hello(name);
                Console.WriteLine(result);
            }
        }
    }



    public interface IHelloWorld
    {
        Task<string> Hello(string name);
    }
}
