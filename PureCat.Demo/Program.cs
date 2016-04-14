using PureCat.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PureCat.Demo
{
    class Program
    {
        static Random _rand = new Random();
        static void Main(string[] args)
        {
            PureCatClient.Initialize();
            var t = int.Parse(Console.ReadLine());
            for (int i = 0; i < t; i++)
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        var a = DateTime.Now.Second;
                        //Console.WriteLine(DateTime.Now);

                        PureCatClient.DoTransaction("SQL", nameof(AtomicTest), AtomicTest);

                        var context = PureCatClient.DoTransaction("Do", nameof(DoTest), DoTest);

                        var b = DateTime.Now.Second;

                        PureCatClient.DoTransaction("Do", nameof(Add), () => Add(a, b, context));

                        //Thread.Sleep(5000);
                    }
                });
            Console.ReadLine();
        }

        static void AtomicTest()
        {
            PureCatClient.LogEvent("SQL", nameof(AtomicTest), "0", "123444433");
        }

        static CatContext DoTest()
        {
            var times = _rand.Next(10);
            //Thread.Sleep(times);
            PureCatClient.LogEvent("Do", nameof(DoTest), "0", $"sleep {times}");

            return PureCatClient.LogRemoteCallClient("callAdd");
        }

        static void Add(int a, int b, CatContext context = null)
        {
            //Thread.Sleep(_rand.Next(10));
            PureCatClient.LogRemoteCallServer(context);
            PureCatClient.LogEvent("Do", nameof(Add), "0", $"{a} + {b} = {a + b}");

            Task.Factory.StartNew(() => PureCatClient.DoTransaction("Do", nameof(Add2), () => PureCatClient.LogRemoteCallClient("callAdd2")));
        }
        static void Add2(int a, int b, CatContext context = null)
        {
            // Thread.Sleep(_rand.Next(10));
            PureCatClient.LogRemoteCallServer(context);
            PureCatClient.LogEvent("Do", nameof(Add2), "0", $"{a} + {b} = {a + b}");
        }
    }
}
