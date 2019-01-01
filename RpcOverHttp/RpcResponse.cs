using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RpcResponse
    {
        public RpcResponse(Stream stream, int seqid, int code)
        {
            ResponseStream = stream;
            this.SequenceId = seqid;
            this.Code = code;
        }

        public int SequenceId { get; set; }
        public int Code { get; set; }
        public string GetResponseText(Encoding encoding)
        {
            return new StreamReader(ResponseStream,true).ReadToEnd();
        }
        public Stream ResponseStream { get; private set; }
    }
}
