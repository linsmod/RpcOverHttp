using RpcOverHttp.SelfHost;
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
        bool stopRequested;
        public void Start(string urlPrefix)
        {
            httpListener = null;
            httpListener = new HttpListener();
            httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            httpListener.Prefixes.Add(urlPrefix);
            httpListener.Start();
            new Thread(new ThreadStart(delegate
            {
                while (!stopRequested)
                {
                    try
                    {
                        HttpListenerContext ctx = httpListener.GetContext();
                        Task.Factory.StartNew(new Action<object>(rpcServer.ProcessRequestInternal), new SystemNetHttpContext(ctx));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error:" + ex.Message);
                    }
                }
            }))
            { IsBackground = true }.Start();
        }

        internal void Stop()
        {
            stopRequested = true;
            this.httpListener.Stop();
        }
    }
}
