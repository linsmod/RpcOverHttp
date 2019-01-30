using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RpcOverHttp.WebHost
{
    public abstract class RpcServerHttpHandler : IHttpHandler
    {
        static IRpcServer server;
        static object lockObj = new object();
        public abstract void InitRpcServer(IRpcServer server);
        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
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
            IRpcHttpContext ctx = new WebHost.SystemWebHttpContext(context);
            if (!string.IsNullOrEmpty(ctx.Request.UserAgent)
                && ctx.Request.UserAgent.IndexOf("RpcOverHttp", StringComparison.OrdinalIgnoreCase) != -1)
            {
                context.Response.TrySkipIisCustomErrors = true;
                server.ProcessRequest(ctx);
            }
            else if (ctx.IsWebSocketRequest)
            {
                ctx.AcceptWebSocket(server.ProcessWebsocketRequest);
            }
        }
    }
    internal class RpcServerHttpHandlerInternal : RpcServerHttpHandler
    {
        private RpcServerHttpModule module;

        public RpcServerHttpHandlerInternal(RpcServerHttpModule module)
        {
            this.module = module;
        }
        public override void InitRpcServer(IRpcServer server)
        {
            module.InitRpcServer(server);
        }
    }
    public abstract class RpcServerHttpModule : IHttpModule
    {
        RpcServerHttpHandler handler;
        public void Dispose()
        {

        }
        public void Init(HttpApplication context)
        {
            handler = new RpcServerHttpHandlerInternal(this);
            context.AuthenticateRequest += Context_AuthenticateRequest;
        }

        private void Context_AuthenticateRequest(object sender, EventArgs e)
        {
            var application = sender as HttpApplication;
            if (application.Request.UserAgent!=null && application.Request.UserAgent.Contains("RpcClient"))
            {
                application.Response.ClearHeaders();
                handler.ProcessRequest(application.Context);
            }
        }

        public abstract void InitRpcServer(IRpcServer server);
    }
}
