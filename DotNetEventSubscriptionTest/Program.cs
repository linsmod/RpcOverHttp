using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace DotNetEventSubscriptionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string v = "1";
            var v1 = JToken.Parse(v).ToObject(typeof(string));
            var v2 = JToken.Parse(v).ToObject(typeof(Int64));
            var v3 = JToken.Parse(v).ToObject(typeof(Int32));
            var v4 = JToken.Parse(v).ToObject(typeof(Int16));
            var v5 = JToken.Parse(v).ToObject(typeof(UInt64));
            var v6 = JToken.Parse(v).ToObject(typeof(UInt32));
            var v7 = JToken.Parse(v).ToObject(typeof(UInt16));
            var v8 = JToken.Parse(v).ToObject(typeof(Single));
            var v9 = JToken.Parse(v).ToObject(typeof(Decimal));
            var v0 = JToken.Parse(v).ToObject(typeof(Double));
            var v11 = JToken.Parse(v).ToObject(typeof(Byte)); ;
            var v22 = JToken.Parse(v).ToObject(typeof(SByte));
            var sender = new Sender();
            for (int i = 0; i < 5; i++)
            {
                new Receiver(i, sender);
            }
            sender.DoGetId();
            Console.Read();
        }
        public class Sender
        {
            public event Func<string> getId;
            public void DoGetId()
            {
                if (getId != null)
                {
                    var id = getId();
                    Console.WriteLine("Sender GetId Returns:" + id);
                }
            }
        }
        public class Receiver
        {
            private Sender sender;
            private int id;
            public Receiver(int id, Sender sender)
            {
                this.id = id;
                this.sender = sender;
                sender.getId += Sender_getId;
            }

            private string Sender_getId()
            {
                Thread.Sleep(1000 - id * 10);
                Console.WriteLine("Receiver {0} Send Id {1}", id, id);
                return id.ToString();
            }
        }
    }
}
