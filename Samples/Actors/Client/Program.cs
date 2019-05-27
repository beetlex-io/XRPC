using BeetleX.XRPC.Clients;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventNext;
namespace Client
{
    class Program
    {
        static XRPCClient client;
        static int mCount;
        static void Main(string[] args)
        {

            client = new XRPCClient("192.168.2.18", 9090);
            client.Connect();
            client.NetError = (c, e) =>
            {
                Console.WriteLine(e.Error.Message);
            };
            client.TimeOut = 10000;
            Test(50, 1000);
            Console.Read();
        }

        static async void Test(int concurrent, int requests)
        {
            mCount = 0;
            IAmountService henry = client.Create<IAmountService>("henry");
            IAmountService ken = client.Create<IAmountService>("ken");
            Console.WriteLine($"[C:{concurrent}|R:{requests}]Testing ");
            List<Task> tasks = new List<Task>();
            double start = EventCenter.Watch.ElapsedMilliseconds;
            for (int i = 0; i < concurrent; i++)
            {
                var task = Task.Run(async () =>
                {
                    for (int k = 0; k < requests; k++)
                    {
                        await henry.Income(10);
                        System.Threading.Interlocked.Increment(ref mCount);
                        await ken.Income(10);
                        System.Threading.Interlocked.Increment(ref mCount);
                    }

                });
                tasks.Add(task);

                task = Task.Run(async () =>
                {
                    for (int k = 0; k < requests; k++)
                    {
                        await henry.Payout(10);
                        System.Threading.Interlocked.Increment(ref mCount);
                        await ken.Payout(10);
                        System.Threading.Interlocked.Increment(ref mCount);
                    }

                });
                tasks.Add(task);


                task = Task.Run(async () =>
                {
                    for (int k = 0; k < requests; k++)
                    {

                        await ken.Income(10);
                        System.Threading.Interlocked.Increment(ref mCount);
                    }

                });
                tasks.Add(task);

                task = Task.Run(async () =>
                {
                    for (int k = 0; k < requests; k++)
                    {

                        await ken.Payout(10);
                        System.Threading.Interlocked.Increment(ref mCount);
                    }

                });
                tasks.Add(task);

            }
            await Task.WhenAll(tasks.ToArray());

            double useTime = EventCenter.Watch.ElapsedMilliseconds - start;
            Console.WriteLine($"Completed count:{mCount}|use time:{useTime}|rps:{(mCount / useTime * 1000d):###.00} |henry:{await henry.Get()},ken:{await ken.Get()}");

        }
    }

    public interface IAmountService
    {
        Task<long> Income(long amount);
        Task<long> Payout(long amount);
        Task<long> Get();
    }
}
