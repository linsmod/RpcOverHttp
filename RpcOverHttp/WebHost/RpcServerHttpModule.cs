using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RpcOverHttp.WebHost
{
    public abstract class RpcServerHttpModule : IHttpModule
    {
        static IRpcServer server;
        static object lockObj = new object();
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            if (server == null)
            {
                lock (lockObj)
                {
                    if (server == null)
                    {
                        server = new RpcServer();
                        this.InitRpcServer(server);
                        (server as RpcServer).Start();
                    }
                }
            }
            context.BeginRequest += Context_BeginRequest;
        }

        public abstract void InitRpcServer(IRpcServer server);

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            var ctx = sender as HttpApplication;
            if (!string.IsNullOrEmpty(ctx.Request.UserAgent)
                && ctx.Request.UserAgent.IndexOf("RpcOverHttp", StringComparison.OrdinalIgnoreCase) != -1)
            {
                server.ProcessRequest(new WebHost.SystemWebHttpContext(ctx));
                ctx.CompleteRequest();
            }
        }
    }
}
