# XRPC
dotnet high performance remote interface invoke(RPC) communication components,implemente millions RPS remote interface method calls.
https://ikende.com/doc/
## v0.3.3
Reference EventNext support create remote actors
## Install Packet
``` 
Install-Package BeetleX.XRPC -Version x
```
## Server
``` csharp
class Program
{
   private static XRPCServer mXRPCServer;
   static void Main(string[] args)
   {
        mXRPCServer = new XRPCServer();
        //mXRPCServer.ServerOptions.DefaultListen.Port = 80;
        mXRPCServer.Register(typeof(Program).Assembly);
        mXRPCServer.Open();
        Console.Read();
    }
}
```
## Server controller
``` csharp
    [Controller(typeof(IUserService))]
    public class UserService : IUserService
    {
        public Task<User> Add(string name, string email, string city, string remark)
        {
            User user = new User();
            user.Name = name;
            user.EMail = email;
            user.City = city;
            user.Remark = remark;
            return Task.FromResult(user);
        }

        public Task<List<User>> List(int count)
        {
            List<User> result = new List<User>();
            for (int i = 0; i < count; i++)
            {
                User user = new User();
                user.ID = Guid.NewGuid().ToString("N");
                user.City = "GuangZhou";
                user.EMail = "Henryfan@msn.com";
                user.Name = "henryfan";
                user.Remark = "http://ikende.com";
                result.Add(user);
            }
            return Task.FromResult(result);
        }

        public bool Login(string name, string pwd)
        {
            return (name == "admin" && pwd == "123456");
        }

        public User Modify(User user)
        {
            return user;
        }

        public void Save()
        {
            Console.WriteLine("user saved");
        }
    }
```
## Client
``` csharp
client = new XRPCClient("localhost", 9090);
client.Connect();
client.NetError = (c, e) =>
{
      Console.WriteLine(e.Error.Message);
};
client.TimeOut = 10000;
```
``` csharp
var api = client.Create<IUserService>();
var lresult = await api.Login("admin", "123456");
Console.WriteLine(lresult);
var result = await api.Add("henry", "henryfan@msn.com", "gz", "http://github.com");
Console.WriteLine($"{result.Name}\t{result.EMail}\t{result.City}\t{result.Remark}");
await api.Save();
Console.WriteLine("save completed");
User user = new User();
user.ID = Guid.NewGuid().ToString("N");
user.Name = "henry";
user.EMail = "henryfan@msn.com";
user.City = "GuangZhou";
user.Remark = "http://github.com/ikende";
result = await api.Modify(user);
Console.WriteLine($"{result.Name}\t{result.EMail}\t{result.City}\t{result.Remark}");
var items = await api.List(5);
foreach(var item in items)
{
    Console.WriteLine($"{item.Name}\t{item.EMail}\t{item.City}\t{item.Remark}");
}
```
## Performance
`
Server:E3-1230V2 16Gb Bandwidth：10Gb sysetm:windows 2008
`
![](https://raw.githubusercontent.com/IKende/XRPC/master/test_report.png)
[https://github.com/IKende/XRPC/tree/master/Samples/Performance](https://github.com/IKende/XRPC/tree/master/Samples/Performance)
