using RpcOverHttp.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetEventSubscriptionTest
{
    class BlockingQueueTest
    {
        static BlockingQueue<object> values = new BlockingQueue<object>();
        public static void Main(string[] args)
        {
            new Thread(ComsumerThread) { IsBackground = true }.Start();
            new Thread(EnqueueThread) { IsBackground = true }.Start();
            Console.Read();
        }
        static void ComsumerThread()
        {
            object value;
            while ((value = values.Dequeue()) != null)
            {
                Console.WriteLine(value);
                Thread.Sleep(1000);
            }
            Console.WriteLine("ComsumerThread end");
        }
        static void EnqueueThread()
        {
            values.Enqueue(100);
            Thread.Sleep(1000);
            for (int i = 0; i < 10; i++)
            {
                values.Enqueue(i);
                Thread.Sleep(20);
            }
        }
    }
}
