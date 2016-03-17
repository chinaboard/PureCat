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
            while (true)
            {
                var a = DateTime.Now.Second;
                Console.WriteLine(DateTime.Now);
                var context = PureCatClient.DoTransaction("Do", nameof(DoTest), DoTest);

                var b = DateTime.Now.Second;

                PureCatClient.DoTransaction("Do", nameof(Add), () => Add(a, b, context));
            }
        }


        static CatContext DoTest()
        {
            var times = _rand.Next(1000);
            Thread.Sleep(times);
            PureCatClient.LogEvent("Do", nameof(DoTest), "0", $"sleep {times}");
            return PureCatClient.LogRemoteCallClient("callAdd");
        }

        static void Add(int a, int b, CatContext context = null)
        {
            PureCatClient.LogRemoteCallServer(context);
            PureCatClient.LogEvent("Do", nameof(Add), "0", $"{a} + {b} = {a + b}");
        }
    }
}
