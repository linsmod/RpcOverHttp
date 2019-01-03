using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace RpcOverHttp.WebHost
{
    internal class SystemWebHttpContext : IRpcHttpContext
    {
        private HttpContext ctx;

        public SystemWebHttpContext(HttpContext ctx)
        {
            this.ctx = ctx;
        }

        Func<Func<AspNetWebSocketContext, Task>, AspNetWebSocketContext> x;

        public void AcceptWebSocket(Func<IRpcWebSocketContext, Task> userFunc)
        {
            ctx.Items["wsUserFunc"] = userFunc;
            ctx.AcceptWebSocketRequest(ProcessRequest, new AspNetWebSocketOptions { SubProtocol = "rpc" });
        }

        public bool IsWebSocketRequest
        {
            get
            {
                return ctx.IsWebSocketRequest;
            }
        }

        private Task ProcessRequest(AspNetWebSocketContext ctx)
        {
            var userFunc = ctx.Items["wsUserFunc"] as Func<IRpcWebSocketContext, Task>;
            return userFunc(new SystemWebWebSocketContext(ctx));
        }

        public class AspNetWebSocketContextWrapper : AspNetWebSocketContext
        {
            private Func<IRpcWebSocketContext> wsctxRetriver;

            public AspNetWebSocketContextWrapper(Func<IRpcWebSocketContext> wsctxRetriver)
            {
                this.wsctxRetriver = wsctxRetriver;
            }

        }

        IRpcHttpRequest IRpcHttpContext.Request
        {
            get
            {
                return new SystemWebHttpRequest(ctx.Request);
            }
        }

        IRpcHttpResponse IRpcHttpContext.Response
        {
            get
            {
                return new SystemWebHttpResponse(ctx.Response);
            }
        }
    }
    internal class SystemWebHttpRequest : IRpcHttpRequest
    {
        private HttpRequest request;

        public SystemWebHttpRequest(HttpRequest request)
        {
            this.request = request;
        }

        long IRpcHttpRequest.ContentLength64
        {
            get
            {
                return request.ContentLength;
            }
        }

        NameValueCollection IRpcHttpRequest.Headers
        {
            get
            {
                return request.Headers;
            }
        }

        Stream IRpcHttpRequest.InputStream
        {
            get
            {
                return request.InputStream;
            }
        }

        bool IRpcHttpRequest.KeepAlive
        {
            get
            {
                return request.Headers["Connection"] != null && request.Headers["Connection"].Equals("keep-alive", StringComparison.OrdinalIgnoreCase);
            }
        }

        Uri IRpcHttpRequest.Url
        {
            get
            {
                return request.Url;
            }
        }
    }

    internal class SystemWebHttpResponse : IRpcHttpResponse
    {
        private HttpResponse response;

        public SystemWebHttpResponse(HttpResponse response)
        {
            this.response = response;
        }

        IRpcHttpHeaderCollection IRpcHttpResponse.Headers
        {
            get
            {
                return new SystemWebHttpHeaderCollection(response.Headers);
            }
        }

        bool IRpcHttpResponse.KeepAlive
        {
            get
            {
                return response.Headers["Connection"] != null && response.Headers["Connection"].Equals("keep-alive");
            }

            set
            {
                if (value)
                {
                    response.Headers["Connection"] = "keep-alive";
                }
                else
                {
                    response.Headers["Connection"] = "close";
                }
            }
        }

        Stream IRpcHttpResponse.OutputStream
        {
            get
            {
                return response.OutputStream;
            }
        }

        bool IRpcHttpResponse.SendChunked
        {
            get
            {
                return response.Headers["Transfer-Encoding"] != null && response.Headers["Transfer-Encoding"].Equals("chunked");
            }

            set
            {
                if (value)
                {
                    response.Headers["Transfer-Encoding"] = "chunked";
                    response.Headers.Remove("Content-Length");
                }
                else
                {
                    response.Headers.Remove("Transfer-Encoding");
                }
            }
        }

        int IRpcHttpResponse.StatusCode
        {
            get
            {
                return response.StatusCode;
            }

            set
            {
                response.StatusCode = value;
            }
        }

        string IRpcHttpResponse.StatusDescription
        {
            get
            {
                return response.StatusDescription;
            }

            set
            {
                response.StatusDescription = value;
            }
        }

        void IRpcHttpResponse.AddHeader(string name, string value)
        {
            response.AddHeader(name, value);
        }

        void IRpcHttpResponse.Close()
        {
            response.Close();
        }

        void IRpcHttpResponse.Flush()
        {
            response.Flush();
        }
    }



    internal class SystemWebHttpHeaderCollection : IRpcHttpHeaderCollection
    {
        private NameValueCollection headers;

        public SystemWebHttpHeaderCollection(NameValueCollection headers)
        {
            this.headers = headers;
        }
    }
}
