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
        static void Main(string[] args)
        {
            client = new XRPCClient("localhost", 9090);
            client.Connect();
            client.NetError = (c, e) =>
            {
                Console.WriteLine(e.Error.Message);
            };
            client.TimeOut = 10000;
            Test();
            Console.Read();
        }

        static async void Test()
        {
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
