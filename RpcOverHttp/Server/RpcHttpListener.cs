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
                        HttpListenerContext ctx = httpListener.GetContext();
                        Task.Factory.StartNew(ProcessRequest, ctx);
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
            this.httpListener.Stop();
        }

        private void ProcessRequest(object state)
        {
            var ctx = state as HttpListenerContext;
            ctx.Response.KeepAlive = ctx.Request.KeepAlive;
            ctx.Response.Headers["Server"] = "kisstudio-RpcOverHttp/1.0";
            Stream outputStream = ctx.Response.OutputStream;
            bool acceptGzip = AcceptsGzip(ctx.Request);
            if (acceptGzip)
            {
                ctx.Response.SendChunked = true;
                ctx.Response.AddHeader("Content-Encoding", "gzip");
                outputStream = new GZipStream(outputStream, CompressionMode.Compress, true);
            }
            try
            {
                if (ctx.Request.Url.AbsolutePath == "/metadata") //接口方法定义和方法实现之间的映射信息
                {
                    this.WriteMetadata(ctx, outputStream);
                }
                else if (ctx.Request.Url.AbsolutePath == "/")
                {
                    {
                        ProcessNormalRequest(ctx, outputStream);
                    }
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.StatusDescription = "Not Found";
                }
                outputStream.Flush();
                outputStream.Close();
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.Message, ex.StackTrace);
                ctx.Response.KeepAlive = false;
                ctx.Response.StatusCode = 500;
                ctx.Response.StatusDescription = "Internal Server Error";
                ctx.Response.Close();
            }
        }

        private static bool AcceptsGzip(HttpListenerRequest request)
        {
            string encoding = request.Headers["Accept-Encoding"];
            if (string.IsNullOrEmpty(encoding))
            {
                return false;
            }

            return encoding.Contains("gzip");
        }

        private void ProcessNormalRequest(HttpListenerContext ctx, Stream outputStream)
        {

            RpcError error = new RpcError("rpc server error.", null);
            if (ctx.Request.ContentLength64 > ByteSize.FromMbs(10).TotalBytes)
            {
                error.Message = "request data is limited in 10Mb";
            }
            else
            {
                var requestMeta = ctx.Request.Headers["meta"];
                if (requestMeta != null)
                {
                    //get request metadata
                    IRpcHeadSerializer headSerializer;
                    if (!iocContainer.TryResolve(out headSerializer))
                    {
                        headSerializer = iocContainer.Resolve<IRpcHeadSerializer>("default");
                    }
                    RpcHead head = null;
                    bool deserialize_head_error_obtained = false;
                    bool deserialize_body_error_obtained = false;
                    try
                    {
                        head = headSerializer.Deserialize(requestMeta);
                    }
                    catch (Exception ex)
                    {
                        deserialize_head_error_obtained = true;
                        error.Message = "error on deserialize rpc head metadata. " + ex.Message;
                        error.StackTrace = ex.StackTrace;
                    }
                    if (head != null)
                    {
                        Type itfType = itfTypes.FirstOrDefault(x => x.Namespace == head.Namespace && x.Name == head.TypeName);
                        if (itfType != null)
                        {
                            var itfMethod = (MethodInfo)ReflectionHelper.ResolveMethod(itfType, head.MethodMDToken);
                            if (itfMethod != null)
                            {
                                var parmTypes = itfMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                                bool error_generated = false;
                                IRpcService rpcService = null;

                                //deserialize arguments
                                IRpcDataSerializer serializer;
                                if (!iocContainer.TryResolve(out serializer))
                                {
                                    serializer = iocContainer.Resolve<IRpcDataSerializer>("default");
                                }
                                object[] args = null;
                                try
                                {
                                    args = serializer.Deserialize(ctx.Request.InputStream, parmTypes);
                                }
                                catch (Exception ex)
                                {
                                    deserialize_body_error_obtained = true;
                                    error.Message = "error on deserialize rpc request data. " + ex.Message;
                                    error.StackTrace = ex.StackTrace;
                                }
                                if (!deserialize_body_error_obtained)
                                {
                                    //find the implimentation of the interface
                                    object impl = iocContainer.Resolve(itfType);
                                    object returnVal = null;

                                    //exception handler and authroize handler
                                    var exceptionHandler = iocContainer.Resolve<IExceptionHandler>("default");
                                    var authorizeHandler = iocContainer.Resolve<IAuthroizeHandler>("default");

                                    //process a call for getting user infomation if user code support authroize
                                    rpcService = impl as IRpcService;
                                    var abstractRpcService = impl as RpcService;
                                    if (abstractRpcService != null)
                                    {
                                        abstractRpcService.exceptionHandler = exceptionHandler;
                                        abstractRpcService.authorizeHandler = authorizeHandler;
                                    }
                                    RpcIdentity identity = rpcService != null ? rpcService.Authroize(head.Token) : authorizeHandler.Authroize(head.Token);
                                    var principal = new RpcPrincipal(identity);
                                    Thread.CurrentPrincipal = principal;
                                    if (abstractRpcService != null)
                                    {
                                        abstractRpcService.User = principal;
                                    }
                                    var rpcAdministration = impl as RpcServiceAdministration;
                                    if (rpcAdministration != null)
                                    {
                                        rpcAdministration.Server = this.rpcServer;
                                    }

                                    Type instanceType = impl.GetType();

                                    try
                                    {
                                        Console.WriteLine("[rpc call] {0}.{1}.{2}", head.Namespace, head.TypeName, head.MethodName);
                                        if (!RpcMethodHelper.IsAuthoirzied(itfType, itfMethod, instanceType))
                                        {
                                            error_generated = true;
                                            error = new RpcError("access denied.", null);
                                            ctx.Response.StatusCode = 401;
                                        }
                                        else
                                        {
                                            //execute the call

                                            MethodInfo implMethod = RpcMethodHelper.FindImplMethod(itfType, itfMethod, instanceType);
                                            returnVal = RpcMethodHelper.Invoke(itfType, itfMethod, impl, implMethod, head.EventOp, head.Timeout, head.Token, args);

                                            //dispose resouces like stream
                                            foreach (IDisposable item in args.OfType<IDisposable>())
                                            {
                                                try
                                                {
                                                    item.Dispose();
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        error_generated = true;
                                        if (rpcService != null)
                                        {
                                            try
                                            {
                                                error = rpcService.HandleException(head, ex);
                                            }
                                            catch (Exception ex2)
                                            {
                                                //do not use exceptionHandler here,
                                                //because the rpcService backend ex handler is exceptionHandler by default
                                                //it may cause a deal loop if use exceptionHandler here
                                                error = RpcError.FromException(ex2);
                                            }
                                        }
                                        else
                                        {
                                            error = exceptionHandler.HandleException(head, ex);
                                        }
                                    }
                                    if (!error_generated)
                                    {
                                        try
                                        {
                                            if (itfMethod.ReturnType != typeof(void))
                                            {
                                                if (typeof(Task).IsAssignableFrom(itfMethod.ReturnType))
                                                {
                                                    if (itfMethod.ReturnType.IsGenericType)
                                                    {
                                                        serializer.Serialize(outputStream, new Type[] { itfMethod.ReturnType.GenericTypeArguments[0] }, new object[] { returnVal });
                                                    }
                                                    else
                                                    {
                                                        //no need handle task without a result.
                                                    }
                                                }
                                                else
                                                {
                                                    serializer.Serialize(outputStream, new Type[] { itfMethod.ReturnType }, new object[] { returnVal });
                                                }
                                                IDisposable value = returnVal as IDisposable;
                                                if (value != null)
                                                    try { value.Dispose(); } catch { }
                                            }
                                            return;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("error on writting response, " + ex.Message);
                                            return; // connection is broken. nothing to do.
                                        }
                                    }
                                }
                            }
                            else
                            {
                                error.Message = "invalid rpc request metadata, unknown method under the interface.";
                            }
                        }
                        else
                        {
                            error.Message = "invalid rpc request metadata, unknown interface.";
                        }
                    }
                    else if (!deserialize_head_error_obtained)
                    {
                        error.Message = "invalid rpc request metadata, deserialize failed.";
                    }
                }
                else
                {
                    error.Message = "invalid rpc request metadata";
                }
            }
            Console.WriteLine(error.Message);
            Console.WriteLine(error.StackTrace);
            if (ctx.Response.StatusCode == 200)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.StatusDescription = "Internal Server Error";
            }
            ctx.WriteOutput(outputStream, error);
        }

        private void WriteMetadata(HttpListenerContext ctx, Stream outputStream)
        {
            var md = ReflectionHelper.GetRpcServiceInfo(impls);
            ctx.WriteOutput(outputStream, md);
        }
    }
}
