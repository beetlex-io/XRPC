using BeetleX.XRPC;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventNext;
namespace ServerApp
{
    class Program
    {
        private static XRPCServer mXRPCServer;

        static void Main(string[] args)
        {
            mXRPCServer = new XRPCServer();
            mXRPCServer.Register(typeof(Program).Assembly);
            mXRPCServer.Open();
            Console.Read();
        }
    }


    [Service(typeof(IUserService))]
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

        public Task<bool> Login(string name, string pwd)
        {
            return (name == "admin" && pwd == "123456").ToTask();
        }

        public Task<User> Modify(User user)
        {
            return user.ToTask();
        }

        public Task Save()
        {
            Console.WriteLine("user saved");
            return Task.CompletedTask;
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
