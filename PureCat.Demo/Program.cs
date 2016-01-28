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
            PureCat.Initialize(new Configuration.ClientConfig(new Configuration.Domain("PureCat.Demo"), new Configuration.Server("10.14.40.4")));
            while (true)
            {
                var a = DateTime.Now.Second;
                Console.WriteLine(DateTime.Now);
                var context = PureCat.DoTransaction("Do", nameof(DoTest), DoTest);

                var b = DateTime.Now.Second;
                Task.Factory.StartNew(() =>
                {
                    PureCat.DoTransaction("Do", nameof(Add), () => Add(a, b, context));
                });
            }

        }


        static CatContext DoTest()
        {
            var times = _rand.Next(1000);
            Thread.Sleep(times);
            PureCat.LogEvent("Do", nameof(DoTest), "0", $"sleep {times}");
            return PureCat.LogRemoteCallClient("callAdd");
        }

        static void Add(int a, int b, CatContext context = null)
        {
            PureCat.LogRemoteCallServer(context);
            PureCat.LogEvent("Do", nameof(Add), "0", $"{a} + {b} = {a + b}");
        }
    }
}
