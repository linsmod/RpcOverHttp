using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    /// <summary>
    /// 远程调用的异常
    /// </summary>
    public class RpcException : Exception
    {
        public RpcError Detail { get; private set; }
        public RpcException(string mesage, Exception innerException, RpcErrorLocation location) : base(mesage, innerException)
        {
            this.Location = location;
        }
        public RpcException(string mesage, RpcError detail, RpcErrorLocation location) : base(mesage)
        {
            this.Detail = detail;
            this.Location = location;
        }
        public RpcErrorLocation Location { get; private set; }

        public override string ToString()
        {
            return this.Message + (this.Detail != null ? "\r\n" + this.Detail.ToString() : "");
        }
    }
    /// <summary>
    /// 服务端发送给客户端的异常信息
    /// </summary>
    public class RpcError
    {
        public RpcError() { }
        public RpcError(string message, string stackTrace)
        {
            this.Message = message;
            this.StackTrace = stackTrace;
        }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public static RpcError FromException(Exception ex)
        {
            return new RpcError(ex.Message, ex.StackTrace);
        }
        public override string ToString()
        {
            return Message + (string.IsNullOrEmpty(StackTrace) ? StackTrace : ("\r\n" + StackTrace));
        }
    }
    public enum RpcErrorLocation
    {
        Local,
        Remote
    }
}
