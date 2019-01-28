using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetEventSubscriptionTest
{
    class Program
    {
        static void Main(string[] args)
        {
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
