using RpcOverHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public abstract class RpcService : IRpcService
    {
        internal IExceptionHandler exceptionHandler;
        internal IAuthroizeHandler authorizeHandler;
        public RpcPrincipal User { get; internal set; }

        public RpcService()
        {
        }

        public virtual RpcIdentity Authroize(string token)
        {
            return authorizeHandler.Authroize(token);
        }

        public virtual RpcError HandleException(RpcHead head, Exception ex)
        {
            return exceptionHandler.HandleException(head, ex);
        }
    }
    internal class DefaultExceptionHandler : IExceptionHandler
    {
        public RpcError HandleException(RpcHead head, Exception ex)
        {
            var ae = ex as AggregateException;
            if (ae != null)
            {
                return RpcError.FromException(ex.InnerException);
            }
            var tie = ex as TargetInvocationException;
            if (tie != null)
            {
                return RpcError.FromException(ex.InnerException);
            }
            return RpcError.FromException(ex);
        }
    }
    internal class DefaultAuthroizeHandler : IAuthroizeHandler
    {
        public RpcIdentity Authroize(string token)
        {
            return new RpcIdentity("Anonymous", new string[0]);
        }
    }
}
