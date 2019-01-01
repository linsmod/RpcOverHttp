using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    /// <summary>
    /// handle token authroize
    /// </summary>
    public interface IAuthroizeHandler
    {
        /// <summary>
        /// build a RpcIdentity by a token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        RpcIdentity Authroize(string token);
    }

    /// <summary>
    /// handle service exception
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// handle service exception and build a RpcError, which will be send to client
        /// </summary>
        /// <param name="head"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        RpcError HandleException(RpcHead head, Exception ex);
    }

    public interface IRpcService
    {
        RpcIdentity Authroize(string token);
        RpcError HandleException(RpcHead head, Exception ex);
    }
}
