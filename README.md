# XRPC
dotnet high performance remote interface invoke(RPC) communication components,implemente millions RPS remote interface method calls.
## samples
https://github.com/IKende/BeetleX-Samples

## Install Packet
``` 
Install-Package BeetleX.XRPC -Version x
```
## Server
``` csharp
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.UseXRPC(s =>
                {
                    s.ServerOptions.LogLevel = BeetleX.EventArgs.LogType.Trace;
                    s.ServerOptions.DefaultListen.Port = 9090;
                    s.RPCOptions.ParameterFormater = new JsonPacket();//default messagepack
                },
                    typeof(Program).Assembly);
            });
            builder.Build().Run();
        }
    }
```
## Server controller
``` csharp

    public interface IHello
    {
        Task<string> Hello(string name);
    }

    [Service(typeof(IHello))]
    public class HelloImpl : IHello
    {
        public Task<string> Hello(string name)
        {
            return $"hello {name} {DateTime.Now}".ToTask();
        }
    }
```
## Client
``` csharp
            client = new XRPCClient("localhost", 9090);
            client.Options.ParameterFormater = new JsonPacket();//default messagepack
            hello = client.Create<IHello>();
            while(true)
            {
                Console.Write("Enter you name:");
                var name = Console.ReadLine();
                var task = hello.Hello(name);
                task.Wait();
                Console.WriteLine(task.Result);
            }
```

