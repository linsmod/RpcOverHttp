using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.SelfHost
{
    class SystemNetWebSocketContext : IRpcWebSocketContext
    {
        private HttpListenerWebSocketContext ctx;

        public SystemNetWebSocketContext(HttpListenerWebSocketContext ctx)
        {
            this.ctx = ctx;
        }

        WebSocket IRpcWebSocketContext.WebSocket
        {
            get
            {
                return ctx.WebSocket;
            }
        }
    }
}
