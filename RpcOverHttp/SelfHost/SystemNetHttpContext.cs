using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.SelfHost
{
    internal class SystemNetHttpContext : IRpcHttpContext
    {
        private HttpListenerContext ctx;

        public SystemNetHttpContext(HttpListenerContext ctx)
        {
            this.ctx = ctx;
        }

        IRpcHttpRequest IRpcHttpContext.Request
        {
            get
            {
                return new SystemNetHttpRequest(ctx.Request);
            }
        }

        IRpcHttpResponse IRpcHttpContext.Response
        {
            get
            {
                return new SystemNetHttpResponse(ctx.Response);
            }
        }
    }
    internal class SystemNetHttpRequest : IRpcHttpRequest
    {
        private HttpListenerRequest request;

        public SystemNetHttpRequest(HttpListenerRequest request)
        {
            this.request = request;
        }

        long IRpcHttpRequest.ContentLength64
        {
            get
            {
                return request.ContentLength64;
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
                return request.KeepAlive;
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
    internal class SystemNetHttpResponse : IRpcHttpResponse
    {
        private HttpListenerResponse response;

        public SystemNetHttpResponse(HttpListenerResponse response)
        {
            this.response = response;
        }

        IRpcHttpHeaderCollection IRpcHttpResponse.Headers
        {
            get
            {
                return new SystemNetHttpHeaderCollection(response.Headers);
            }
        }

        bool IRpcHttpResponse.KeepAlive
        {
            get
            {
                return response.KeepAlive;
            }

            set
            {
                response.KeepAlive = value;
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
                return response.SendChunked;
            }

            set
            {
                response.SendChunked = value;
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
        }


    }

    internal class SystemNetHttpHeaderCollection : IRpcHttpHeaderCollection
    {
        private WebHeaderCollection headers;

        public SystemNetHttpHeaderCollection(WebHeaderCollection headers)
        {
            this.headers = headers;
        }
    }
}
