using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal static class HttpListenerContextExtension
    {
        public static Encoding Utf8NonBom = new UTF8Encoding(false);
        public static void WriteOutput(this HttpListenerContext ctx, Stream outputStream, object value)
        {
            if (value == null)
                value = "";
            if (value is Stream)
            {
                using (var stream = value as Stream)
                {
                    stream.CopyTo(outputStream);
                }
            }
            else if (value is string)
            {
                using (var sw = new StreamWriter(outputStream, Utf8NonBom, 2048,true))
                {
                    sw.Write(value as string);
                }
            }
            else
            {
                WriteOutput(ctx, outputStream, JsonHelper.ToString(value));
            }
        }
    }
}
