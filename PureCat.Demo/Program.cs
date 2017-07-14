using PureCat.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PureCat.Demo
{
    internal class Program
    {
        private static Random _rand = new Random();

        private static void Main(string[] args)
        {
            PureCatClient.Initialize();
            while (true)
            {
                var a = DateTime.Now.Second;
                Console.WriteLine(DateTime.Now);
                var context = PureCatClient.DoTransaction("Do", nameof(DoTest), DoTest);

                var b = DateTime.Now.Second;

                PureCatClient.DoTransaction("Do", nameof(Add), () => Add(a, b, context));

                Thread.Sleep(5000);
            }
        }

        private static CatContext DoTest()
        {
            var times = _rand.Next(1000);
            Thread.Sleep(times);
            PureCatClient.LogEvent("Do", nameof(DoTest), "0", $"sleep {times}");
            return PureCatClient.LogRemoteCallClient("callAdd");
        }

        private static void Add(int a, int b, CatContext context = null)
        {
            Thread.Sleep(_rand.Next(1000));
            PureCatClient.LogRemoteCallServer(context);
            PureCatClient.LogEvent("Do", nameof(Add), "0", $"{a} + {b} = {a + b}");

            Task.Factory.StartNew(() => PureCatClient.DoTransaction("Do", nameof(Add2), () => PureCatClient.LogRemoteCallClient("callAdd2")));
        }

        private static void Add2(int a, int b, CatContext context = null)
        {
            Thread.Sleep(_rand.Next(1000));
            PureCatClient.LogRemoteCallServer(context);
            PureCatClient.LogEvent("Do", nameof(Add2), "0", $"{a} + {b} = {a + b}");
        }
    }
}