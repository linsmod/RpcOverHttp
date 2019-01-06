using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebSockets;

namespace RpcOverHttp.WebHost
{
    class SystemWebWebSocketContext : IRpcWebSocketContext
    {
        private AspNetWebSocketContext ctx;

        public SystemWebWebSocketContext(AspNetWebSocketContext ctx)
        {
            this.ctx = ctx;
        }

        WebSocket IRpcWebSocketContext.WebSocket
        {
            get { return ctx.WebSocket; }
        }

        Uri IRpcWebSocketContext.RequestUri
        {
            get
            {
                return ctx.RequestUri;
            }
        }
    }
}
