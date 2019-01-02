using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public interface IRpcHttpContext
    {
        IRpcHttpRequest Request { get; }
        IRpcHttpResponse Response { get; }
    }
    public interface IRpcHttpRequest
    {
        long ContentLength64 { get; }
        Uri Url { get; }
        bool KeepAlive { get; }
        Stream InputStream { get; }
        NameValueCollection Headers { get; }
    }

    public interface IRpcHttpResponse
    {
        bool SendChunked { get; set; }
        int StatusCode { get; set; }
        string StatusDescription { get; set; }

        void AddHeader(string name, string value);

        void Close();

        bool KeepAlive { get; set; }

        Stream OutputStream { get; }
        IRpcHttpHeaderCollection Headers { get; }

        void Flush();
    }

    public interface IRpcHttpHeaderCollection
    {
    }
}
