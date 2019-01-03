using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public interface IRpcWebSocketContext
    {
        WebSocket WebSocket { get; }
    }
}
