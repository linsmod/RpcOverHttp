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
        public RpcServerHttpHandler()
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
        }
        public abstract void InitRpcServer(IRpcServer server);
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
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
    //public abstract class RpcServerHttpModule : IHttpModule
    //{
    //    static IRpcServer server;
    //    static object lockObj = new object();
    //    public void Dispose()
    //    {
    //    }

    //    public void Init(HttpApplication context)
    //    {
    //        if (server == null)
    //        {
    //            lock (lockObj)
    //            {
    //                if (server == null)
    //                {
    //                    server = new RpcServer();
    //                    this.InitRpcServer(server);
    //                    (server as RpcServer).Start();
    //                }
    //            }
    //        }
    //        //context.BeginRequest += Context_BeginRequest;
    //        context.MapRequestHandler += Context_MapRequestHandler;
    //        context.
    //    }

    //    private void Context_MapRequestHandler(object sender, EventArgs e)
    //    {
    //        var application = sender as HttpApplication;
    //        IRpcHttpContext ctx = new WebHost.SystemWebHttpContext(application.Context);
    //        if (!string.IsNullOrEmpty(ctx.Request.UserAgent)
    //            && ctx.Request.UserAgent.IndexOf("RpcOverHttp", StringComparison.OrdinalIgnoreCase) != -1)
    //        {
    //            application.Response.TrySkipIisCustomErrors = true;
    //            server.ProcessRequest(ctx);
    //            application.CompleteRequest();
    //        }
    //        else if (ctx.IsWebSocketRequest)
    //        {
    //            ctx.AcceptWebSocket(server.ProcessWebsocketRequest);
    //        }
    //        //else if (ctx.Request.Headers["Connection"] != null
    //        //    && ctx.Request.Headers["Connection"].IndexOf("Upgrade", StringComparison.OrdinalIgnoreCase) != -1
    //        //    && ctx.Request.Headers["Upgrade"] != null
    //        //    && ctx.Request.Headers["Upgrade"].IndexOf("WebSocket", StringComparison.OrdinalIgnoreCase) != -1
    //        //    )
    //        //{
    //        //    ctx.Response.StatusCode = 101; // to make iis websocket module handle ws shakehands
    //        //    ctx.Response.Flush();
    //        //}
    //    }

    //    public abstract void InitRpcServer(IRpcServer server);

    //    private void Context_BeginRequest(object sender, EventArgs e)
    //    {
    //    }
    //}
}
