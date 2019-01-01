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
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RpcDynamicProxyFactory
    {
        DynamicProxyFactory<RpcOverHttpDaynamicProxy> factory = new DynamicProxyFactory<RpcOverHttpDaynamicProxy>(new DynamicInterfaceImplementor());
        internal WebProxy webProxy;
        internal TinyIoC.TinyIoCContainer iocContainer;
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
        public string GetUrl(string token)
        {
            return this.ApiUrl + "download?token=" + token;
        }
    }
    internal class RpcOverHttpDaynamicProxy : DynamicProxy
    {
        string token;
        RpcDynamicProxyFactory factory;
        MethodInfo methodMakeGenericTask;
        private int rpcTimeout = 120 * 1000;
        public RpcOverHttpDaynamicProxy(RpcDynamicProxyFactory factory, string token)
        {
            this.token = token;
            this.factory = factory;
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
                throw new RpcException("rpc request error. ", rpcError, RpcErrorLocation.Local);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var rpcError = new RpcError(ex.Message, new StackTrace(ex).ToString());
                throw new RpcException("rpc request error. ", rpcError, RpcErrorLocation.Local);
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


            IRpcHeadSerializer headSerializer;
            if (!this.factory.iocContainer.TryResolve(out headSerializer))
            {
                headSerializer = this.factory.iocContainer.Resolve<IRpcHeadSerializer>("default");
            }
            httprequest.Headers.Add("meta", headSerializer.Serialize(request as RpcHead));
            httprequest.Headers.Add("Accept-Encoding", "gzip");
            httprequest.Proxy = factory.webProxy;
            var writeStream = httprequest.GetRequestStream();

            IRpcDataSerializer serializer;
            if (!this.factory.iocContainer.TryResolve(out serializer))
            {
                serializer = this.factory.iocContainer.Resolve<IRpcDataSerializer>("default");
            }
            serializer.Serialize(writeStream, method.GetParameters().Select(x => x.ParameterType).ToArray(), request.Arguments);
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

        private RpcResponse HandleResponse(HttpWebResponse httpresp)
        {
            var contentEncoding = httpresp.Headers["Content-Encoding"];
            var transferEncoding = httpresp.Headers["Transfer-Encoding"];
            bool gzip = contentEncoding != null && contentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase);
            bool chunked = transferEncoding != null && transferEncoding.Equals("chunked", StringComparison.OrdinalIgnoreCase);
            var responseStream = httpresp.GetResponseStream();
            var readStream = gzip ? new GZipStream(httpresp.GetResponseStream(), CompressionMode.Decompress, false) : httpresp.GetResponseStream();
            responseStream.ReadTimeout = this.rpcTimeout;
            if (httpresp.StatusCode == HttpStatusCode.OK || httpresp.StatusCode == HttpStatusCode.NoContent)
            {
                return new RpcResponse(readStream, 0, 0);
            }
            else if (httpresp.StatusCode == HttpStatusCode.InternalServerError
                || httpresp.StatusCode == HttpStatusCode.Unauthorized)
            {
                var streamReader = new StreamReader(readStream);
                var errorDetail = streamReader.ReadToEnd();
                var detail = JsonHelper.FromString<RpcError>(errorDetail);
                throw new RpcException($"rpc request error. http.response.status_code={(int)httpresp.StatusCode}. see RpcException.Detail for more information.", detail, RpcErrorLocation.Remote);
            }
            else
            {
                throw new RpcException($"rpc request error. http.response.status_code={(int)httpresp.StatusCode}.", (RpcError)null, RpcErrorLocation.Remote);
            }
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
                                throw new RpcException("failed to deserialize response data, check inner exception for more information.", ex, RpcErrorLocation.Local);
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
                            throw new RpcException("failed to deserialize response data, check inner exception for more information.", ex, RpcErrorLocation.Local);
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

        protected override bool TrySetEvent(Type interfaceType, string name, object value)
        {
            EventInfo e = interfaceType.GetEvent(name);
            if (value != null) //add_EventHandler
            {
                object result;
                this.TryInvokeMember(interfaceType, e.AddMethod.MetadataToken, true, new object[] { (value as EventHandler).Method.MetadataToken }, out result);
            }
            else //remove_EventHandler
            {
                object result;
                this.TryInvokeMember(interfaceType, e.RemoveMethod.MetadataToken, true, new object[] { value }, out result);
            }
            return true;
        }

        protected override bool TrySetMember(Type interfaceType, string name, object value)
        {
            throw new NotImplementedException();
        }
    }
}
