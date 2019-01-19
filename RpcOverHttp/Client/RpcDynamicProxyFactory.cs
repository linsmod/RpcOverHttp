using DynamicProxyImplementation;
using Newtonsoft.Json.Linq;
using RpcOverHttp.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RpcDynamicProxyFactory
    {
        DynamicProxyFactory<RpcOverHttpDaynamicProxy> factory = new DynamicProxyFactory<RpcOverHttpDaynamicProxy>(new DynamicInterfaceImplementor());
        internal WebProxy webProxy;
        internal TinyIoC.TinyIoCContainer iocContainer;
        internal string websocketServer;

        internal RpcDynamicProxyFactory(Uri url, WebProxy proxy, TinyIoC.TinyIoCContainer iocContainer)
        {
            this.ApiUrl = url;
            webProxy = proxy;
            this.iocContainer = iocContainer;
        }
        public TInterface GetProxy<TInterface>(string token = null)
        {
            return factory.CreateDynamicProxy<TInterface>(this, token);
        }
        public Uri ApiUrl { get; set; }
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; internal set; }

        public string GetUrl(string token)
        {
            return this.ApiUrl + "download?token=" + token;
        }
    }
    internal class RpcOverHttpDaynamicProxy : DynamicProxy
    {
        string token;
        bool wsconnected = false;
        ClientWebSocket clientWebSocket = new ClientWebSocket();
        private object wsLock = new object();
        RpcDynamicProxyFactory factory;
        private Version version;
        MethodInfo methodMakeGenericTask;
        private int rpcTimeout = 120 * 1000;
        private Guid instanceId = Guid.NewGuid();
        public RpcOverHttpDaynamicProxy(RpcDynamicProxyFactory factory, string token)
        {
            this.token = token;
            this.factory = factory;
            this.version = typeof(RpcResponse).Assembly.GetName().Version;
        }

        protected override bool TryGetMember(Type interfaceType, string name, out object result)
        {
            result = null;
            return true;
        }
        protected override bool TryInvokeMember(Type interfaceType, int mdToken, bool eventOp, object[] args, out object result)
        {
            try
            {
                var method = interfaceType.Assembly.ManifestModule.ResolveMethod(mdToken) as MethodInfo;
                var request = new RpcRequest();
                request.Id = RpcRequestId.Next();
                request.Namespace = interfaceType.Namespace;
                request.TypeName = interfaceType.Name;
                request.MethodName = method.Name;
                request.MethodMDToken = mdToken;
                request.Token = this.token;
                request.InstanceId = this.instanceId;
                request.EventOp = eventOp;
                request.Arguments = args;
                var state = new ClientRpcState { Method = method, Request = request };
                if (typeof(Task).IsAssignableFrom(method.ReturnType))
                {
                    Task generated_local_task;
                    if (method.ReturnType.IsGenericType) //Task<T>
                    {
                        generated_local_task = this.MakeTask(typeof(object), method.ReturnType.GenericTypeArguments[0], state) as Task;
                    }
                    else
                    {
                        generated_local_task = new Task((x) => this.Invoke(x), state);
                    }
                    generated_local_task.Start(TaskScheduler.Default);
                    result = generated_local_task;
                }
                else if (method.ReturnType == typeof(void))
                {
                    result = null;
                    InvokeVoid(state);
                }
                else
                    result = Invoke(state);
                return true;
            }
            catch (AggregateException ae)
            {
                var rpcError = new RpcError(ae.InnerException.Message, new StackTrace(ae.InnerException).ToString());
                throw new RpcException("rpc request error. ", rpcError, RpcErrorLocation.Client);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var rpcError = new RpcError(ex.Message, new StackTrace(ex).ToString());
                throw new RpcException("rpc request error. ", rpcError, RpcErrorLocation.Client);
            }
        }

        public Task<TResult> MakeGenericTask<TResult>(object state)
        {
            return new Task<TResult>(this.InvokeResult<TResult>, state);
        }

        private object MakeTask(Type typeIn, Type resultType, object state)
        {
            if (methodMakeGenericTask == null)
            {
                var _this = typeof(RpcOverHttpDaynamicProxy);
                methodMakeGenericTask = _this.GetMethod("MakeGenericTask").MakeGenericMethod(resultType);
            }
            return methodMakeGenericTask.Invoke(this, new object[] { state });
        }

        public TResult InvokeResult<TResult>(object state)
        {
            return (TResult)this.Invoke(state);
        }

        bool ServerCertificateCustomValidationCallback(HttpRequestMessage m, X509Certificate2 cert, X509Chain chain, SslPolicyErrors error)
        {
            return true;
        }
        internal RpcResponse InvokeResponse(object state)
        {
            var request = (state as ClientRpcState).Request;
            var method = (state as ClientRpcState).Method;

            ServicePointManager.Expect100Continue = false;
            HttpWebRequest httprequest = WebRequest.CreateHttp(factory.ApiUrl);
            httprequest.Method = "POST";
            httprequest.KeepAlive = true;
            httprequest.ServerCertificateValidationCallback = this.factory.ServerCertificateValidationCallback;

            IRpcHeadSerializer headSerializer;
            if (!this.factory.iocContainer.TryResolve(out headSerializer))
            {
                headSerializer = this.factory.iocContainer.Resolve<IRpcHeadSerializer>("default");
            }
            httprequest.UserAgent = "RpcClient-RpcOverHttp/" + this.version;
            httprequest.Headers.Add("meta", headSerializer.Serialize(request as RpcHead));
            httprequest.Headers.Add("Accept-Encoding", "gzip");
            httprequest.Proxy = factory.webProxy;
            var writeStream = httprequest.GetRequestStream();

            IRpcDataSerializer serializer;
            if (!this.factory.iocContainer.TryResolve(out serializer))
            {
                serializer = this.factory.iocContainer.Resolve<IRpcDataSerializer>("default");
            }
            var argTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
            if (request.EventOp)
            {
                FixupRemoteEventKey(argTypes, request.Arguments);
            }
            serializer.Serialize(writeStream, argTypes, request.Arguments);
            writeStream.Flush();
            try
            {
                var httpresp = httprequest.GetResponse() as HttpWebResponse;
                return HandleResponse(httpresp);
            }
            catch (WebException ex)
            {
                var resp = ex.Response as HttpWebResponse;
                if (resp != null)
                {
                    return HandleResponse(resp);
                }
                else
                    throw;
            }
        }

        private void FixupRemoteEventKey(Type[] argTypes, object[] args)
        {
            for (int i = 0; i < argTypes.Length; i++)
            {
                argTypes[i] = typeof(int);
                args[i] = (args[i] as RemoteEventSubscription).GetHashCode();
            }
        }

        private RpcResponse HandleResponse(HttpWebResponse httpresp)
        {
            var contentEncoding = httpresp.Headers["Content-Encoding"];
            var transferEncoding = httpresp.Headers["Transfer-Encoding"];
            bool gzip = contentEncoding != null && contentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase);
            bool chunked = transferEncoding != null && transferEncoding.Equals("chunked", StringComparison.OrdinalIgnoreCase);
            var responseStream = httpresp.GetResponseStream();
            var readStream = gzip ? new GZipStream(httpresp.GetResponseStream(), CompressionMode.Decompress, false) : httpresp.GetResponseStream();
            responseStream.ReadTimeout = this.rpcTimeout;

            Encoding encoding = Encoding.UTF8;
            if (httpresp.StatusCode == HttpStatusCode.OK || httpresp.StatusCode == HttpStatusCode.NoContent)
            {
                return new RpcResponse(readStream, 0, 0);
            }
            else if (httpresp.StatusCode == HttpStatusCode.InternalServerError
                || httpresp.StatusCode == HttpStatusCode.Unauthorized)
            {
                var cnt = GetContent(readStream);
                var detail = JsonHelper.FromString<RpcError>(cnt);
                throw new RpcException($"rpc request error. http.response.status_code={(int)httpresp.StatusCode}. see RpcException.Detail for more information.", detail, RpcErrorLocation.Server)
                {
                    Response = httpresp,
                    ResponseContent = cnt,
                };
            }
            else
            {

                throw new RpcException($"rpc request error. http.response.status_code={(int)httpresp.StatusCode}.", (RpcError)null, RpcErrorLocation.Server)
                {
                    Response = httpresp,
                    ResponseContent = GetContent(readStream)
                };
            }
        }

        private string GetContent(Stream readStream)
        {
            var stream = new MemoryStream();
            readStream.CopyTo(stream);
            stream.Position = 0;
            var streamReader = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var cnt = streamReader.ReadToEnd();
            stream.Position = 0;
            var enc = getEncoding(cnt);
            if (enc != Encoding.UTF8)
            {
                streamReader = new StreamReader(stream, enc);
                cnt = streamReader.ReadToEnd();
            }
            return cnt;
        }

        private Encoding getEncoding(string cnt)
        {
            Encoding encoding = Encoding.UTF8;
            Match match;
            if ((match = Regex.Match(cnt, "<meta(.+)/>")).Success)
            {
                var meta = match.Groups[1].Value;
                if (meta.IndexOf("Content-Type") != -1)
                {
                    if (meta.IndexOf("gb2312") != -1)
                    {
                        encoding = Encoding.GetEncoding("gb2312");
                    }
                    else if (meta.IndexOf("gbk") != -1)
                    {
                        encoding = Encoding.GetEncoding("gbk");
                    }
                    else if (meta.IndexOf("utf-8") != -1)
                    {
                        encoding = Encoding.GetEncoding("utf-8");
                    }
                }
            }
            return encoding;
        }

        private bool HandleException(Exception ex)
        {
            var webEx = ex as WebException;
            if (webEx != null)
            {
                var webresp = webEx.Response as WebResponse;
                if (webresp != null)
                {
                    FromWebException(webEx);
                    return true;
                }
            }
            return false;
        }

        private void InvokeVoid(object state)
        {
            Invoke(state);
        }

        private object Invoke(object state)
        {
            var request = (state as ClientRpcState).Request;
            var method = (state as ClientRpcState).Method;
            var response = this.InvokeResponse(state);

            var returnType = (state as ClientRpcState).Method.ReturnType;
            if (returnType != typeof(void))
            {
                if (response.ResponseStream != null)
                {
                    IRpcDataSerializer serializer = null;
                    if (!this.factory.iocContainer.TryResolve<IRpcDataSerializer>(out serializer))
                    {
                        serializer = this.factory.iocContainer.Resolve<IRpcDataSerializer>("default");
                    }
                    if (typeof(Task).IsAssignableFrom(returnType))
                    {
                        if (returnType.IsGenericType)
                        {
                            try
                            {
                                return serializer.Deserialize(response.ResponseStream, new Type[] { returnType.GenericTypeArguments[0] }).Single();
                            }
                            catch (Exception ex)
                            {
                                throw new RpcException("failed to deserialize response data, check inner exception for more information.", ex, RpcErrorLocation.Client);
                            }
                        }
                        else
                            return null;
                    }
                    else
                    {
                        try
                        {
                            return serializer.Deserialize(response.ResponseStream, new Type[] { returnType }).Single();
                        }
                        catch (Exception ex)
                        {
                            throw new RpcException("failed to deserialize response data, check inner exception for more information.", ex, RpcErrorLocation.Client);
                        }
                    }
                }
                else
                {
                    throw new Exception("response stream is invalid for read.");
                }
            }
            else
                return null;
        }

        private RpcResponse FromWebException(WebException ex)
        {
            var httpResponse = ex.Response as HttpWebResponse;
            if (httpResponse != null)
            {
                var ez_code = int.Parse(httpResponse.Headers["ez_code"]);
                var ez_seqid = int.Parse(httpResponse.Headers["ez_seqid"]);
                var stream = httpResponse.GetResponseStream();
                return new RpcResponse(stream, ez_seqid, ez_code);
            }
            return null;
        }

        /// <summary>
        /// key = interface.method + . + eventhandler.method
        /// </summary>
        Dictionary<RemoteEventSubscription, MethodInfo> subscriptions = new Dictionary<RemoteEventSubscription, MethodInfo>();

        private async Task EventListenerThread()
        {
            IRpcDataSerializer serializer;
            if (!this.factory.iocContainer.TryResolve(out serializer))
            {
                serializer = this.factory.iocContainer.Resolve<IRpcDataSerializer>("default");
            }
            const int maxMessageSize = 1024;
            var receivedDataBuffer = new ArraySegment<Byte>(new Byte[maxMessageSize]);
            while (clientWebSocket.State == WebSocketState.Open)
            {
                var wsResult = await clientWebSocket.ReceiveAsync(receivedDataBuffer, CancellationToken.None);
                using (var inputms = new MemoryStream(receivedDataBuffer.Take(wsResult.Count).ToArray()))
                {
                    //response = eventKey + arguments
                    var keyBytes = new byte[4];
                    inputms.Read(keyBytes, 0, keyBytes.Length);
                    var eventKey = BitConverter.ToInt32(keyBytes, 0);
                    var key = this.subscriptions.Keys.Single(x => x == eventKey);
                    var handlerMethod = this.subscriptions[key];
                    var arguments = serializer.Deserialize(inputms, handlerMethod.GetParameters().Select(x => x.ParameterType).ToArray());

                    IRpcEventHandleResult result =
                        handlerMethod.ReturnType == typeof(void) ?
                        new RpcEventHandleResultVoid() :
                        Activator.CreateInstance(typeof(RpcEventHandleResultGeneral<>).MakeGenericType(handlerMethod.ReturnType)) as IRpcEventHandleResult;

                    object retVal = null;
                    try
                    {
                        retVal = handlerMethod.Invoke(this, arguments);
                        result.Value = retVal;
                    }
                    catch (Exception ex)
                    {
                        result.Error = RpcError.FromException(ex);
                    }
                    using (var outputms = new MemoryStream())
                    {
                        serializer.Serialize(outputms, new Type[] { result.GetType() }, new object[] { result });
                        await clientWebSocket.SendAsync(new ArraySegment<byte>(outputms.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            }
        }

        protected override bool TrySetEvent(Type interfaceType, string name, object value, bool add)
        {
            EventInfo e = TypeHelper.GetEventInfo(interfaceType, name);
            if (!wsconnected)
            {
                lock (wsLock)
                {
                    if (!wsconnected)
                    {
                        clientWebSocket.ConnectAsync(new Uri(this.factory.websocketServer + "?instanceId=" + this.instanceId), CancellationToken.None).Wait();
                        wsconnected = true;
                        Task.Factory.StartNew(EventListenerThread);
                    }
                }
            }

            if (add) //add_EventHandler
            {
                lock (subscriptions)
                {
                    var key = AddSubscription(interfaceType, name, (value as Delegate).Method);
                    object result;
                    this.TryInvokeMember(interfaceType, e.AddMethod.MetadataToken, true, new object[] { key }, out result);
                }
            }
            else //remove_EventHandler
            {
                lock (subscriptions)
                {
                    var key = RemoveSubscription(interfaceType, name, (value as Delegate).Method);
                    object result;
                    this.TryInvokeMember(interfaceType, e.RemoveMethod.MetadataToken, true, new object[] { key }, out result);
                }
            }
            return true;
        }
        private RemoteEventSubscription AddSubscription(Type interfaceType, string eventName, MethodInfo eventHandlerMethod)
        {
            var key = new RemoteEventSubscription(this.instanceId, interfaceType, eventName, eventHandlerMethod);
            var idk = BitConverter.GetBytes(key.GetHashCode());
            if (!subscriptions.ContainsKey(key))
                subscriptions.Add(key, eventHandlerMethod);
            return key;
        }

        private RemoteEventSubscription RemoveSubscription(Type interfaceType, string eventName, MethodInfo eventHandlerMethod)
        {
            var key = new RemoteEventSubscription(this.instanceId, interfaceType, eventName, eventHandlerMethod);
            var idk = BitConverter.GetBytes(key.GetHashCode());
            subscriptions.Remove(key);
            return key;
        }

        protected override bool TrySetMember(Type interfaceType, string name, object value)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "dispose", CancellationToken.None).Wait();
            base.Dispose(disposing);
        }
        protected override bool TryInvokeEventHandler(Type interfaceType, Type handlerType, string name, object[] args, out object resul)
        {
            throw new NotSupportedException("invoke eventhandler in client side is not supported.");
        }
    }
}
