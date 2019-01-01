using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RpcRequestId
    {
        static object lockId = new object();
        static int _id = 10000;
        public static int Next()
        {
            lock (lockId)
            {
                return _id++;
            }
        }
    }
}
