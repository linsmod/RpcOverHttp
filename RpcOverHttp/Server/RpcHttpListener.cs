﻿using RpcOverHttp.SelfHost;
using RpcOverHttp.Serialization;
using RpcOverHttp.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TinyIoC;

namespace RpcOverHttp
{
    class RpcHttpListener
    {
        HttpListener httpListener = new HttpListener();
        private RpcServer rpcServer;
        private IEnumerable<Type> itfTypes;
        private IEnumerable<Type> implTypes;
        private IEnumerable<object> impls;
        private TinyIoCContainer iocContainer;

        public RpcHttpListener(RpcServer rpcServer)
        {
            this.rpcServer = rpcServer;
            this.iocContainer = rpcServer.iocContainer;
            itfTypes = iocContainer.RegisteredTypes.Select(x => x.Type);
            impls = itfTypes.Select(x => iocContainer.Resolve(x));
            implTypes = impls.Select(x => x.GetType());
        }

        public void Start(string urlPrefix)
        {
            httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            httpListener.Prefixes.Add(urlPrefix);
            httpListener.Start();
            new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    try
                    {
                        HttpListenerContext ctx_raw = httpListener.GetContext();
                        IRpcHttpContext ctx = new SystemNetHttpContext(ctx_raw);
                        if (ctx_raw.Request.IsWebSocketRequest)
                        {
                            ctx.AcceptWebSocket(rpcServer.ProcessWebsocketRequest);
                        }
                        else
                        {
                            new Thread(rpcServer.ProcessRequestInternal) { Name = "HttpRequestHandlerThread", IsBackground = true }.Start(ctx);
                            //Task.Factory.StartNew(new Action<object>(rpcServer.ProcessRequestInternal), ctx);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }))
            { IsBackground = true }.Start();
        }

        internal void Stop()
        {
            this.httpListener.Stop();
        }


    }
}
