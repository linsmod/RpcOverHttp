using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class ClientRpcState
    {
        public RpcRequest Request { get; set; }
        public MethodInfo Method { get; set; }
        public bool IsAddEvent { get; internal set; }
    }
}
