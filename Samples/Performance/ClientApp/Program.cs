using BeetleX;
using BeetleX.XRPC.Clients;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        static XRPCClient client;
        static IUserService UserService;
        static int mCount;
        static int[] mUsers = new int[] { 20, 50, 100 };
        static void Main(string[] args)
        {
            client = new XRPCClient("localhost", 9090);
            client.Connect();
            client.NetError = (c, e) =>
            {
                Console.WriteLine(e.Error.Message);
            };
            client.TimeOut = 10000;
            UserService = client.Create<IUserService>();
            Test();
            Console.Read();
        }

        static async void Test()
        {
            foreach (int i in mUsers)
                await Login(i);
            foreach (int i in mUsers)
                await Add(i);
            foreach (int i in mUsers)
                await List(i);

        }

        static async Task Login(int users)
        {
            mCount = 0;
            List<Task> tasks = new List<Task>();
            double start = TimeWatch.GetElapsedMilliseconds();
            for (int i = 0; i < users; i++)
            {
                var item = Task.Run(async () =>
                {
                    for (int k = 0; k < 10000; k++)
                    {
                        var result = await UserService.Login("admin", "123456");
                        System.Threading.Interlocked.Increment(ref mCount);
                    }
                });
                tasks.Add(item);
            }
            Task.WaitAll(tasks.ToArray());
            double useTime = TimeWatch.GetElapsedMilliseconds() - start;
            Console.WriteLine($"[{DateTime.Now:t}][Login][Users:{users}|Count:{mCount}|Times:{useTime:######.000}ms|RPS:{(mCount / useTime * 1000):#######}]");
            await Task.CompletedTask;
        }

        static async Task Add(int users)
        {
            mCount = 0;
            List<Task> tasks = new List<Task>();
            double start = TimeWatch.GetElapsedMilliseconds();
            for (int i = 0; i < users; i++)
            {
                var item = Task.Run(async () =>
                {
                    for (int k = 0; k < 10000; k++)
                    {
                        var result = await UserService.Add("henry", "henryfan@msn.com", "guangzhou", "http://github.com");
                        System.Threading.Interlocked.Increment(ref mCount);
                    }
                });
                tasks.Add(item);
            }
            Task.WaitAll(tasks.ToArray());
            double useTime = TimeWatch.GetElapsedMilliseconds() - start;
            Console.WriteLine($"[{DateTime.Now:t}][Add][Users:{users}|Count:{mCount}|Times:{useTime:######.000}ms|RPS:{(mCount / useTime * 1000):#######}]");
            await Task.CompletedTask;
        }

        static async Task List(int users)
        {
            mCount = 0;
            List<Task> tasks = new List<Task>();
            double start = TimeWatch.GetElapsedMilliseconds();
            for (int i = 0; i < users; i++)
            {
                var item = Task.Run(async () =>
                {
                    for (int k = 0; k < 10000; k++)
                    {
                        var result = await UserService.List(5);
                        System.Threading.Interlocked.Increment(ref mCount);
                    }
                });
                tasks.Add(item);
            }
            Task.WaitAll(tasks.ToArray());
            double useTime = TimeWatch.GetElapsedMilliseconds() - start;
            Console.WriteLine($"[{DateTime.Now:t}][List][Users:{users}|Count:{mCount}|Times:{useTime:######.000}ms|RPS:{(mCount / useTime * 1000):#######}]");
            await Task.CompletedTask;
        }
    }

    public interface IUserService
    {
        Task<bool> Login(string name, string pwd);

        Task<User> Add(string name, string email, string city, string remark);

        Task Save();

        Task<User> Modify(User user);

        Task<List<User>> List(int count);
    }

    [MessagePackObject]
    public class User
    {
        [Key(4)]
        public string ID { get; set; }

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public string City { get; set; }

        [Key(2)]
        public string EMail { get; set; }

        [Key(3)]
        public string Remark { get; set; }
    }
}
