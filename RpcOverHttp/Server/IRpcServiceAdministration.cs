using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.Server
{
    public class RpcServiceAdminAuthorizeAttribute : AuthorizeAttribute
    {
        public override bool IsAuthroized()
        {
            return base.IsAuthroized();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public interface IRpcServiceAdministration
    {
        /// <summary>
        /// touch service using a http head request to warmup it
        /// </summary>
        void Touch();
        /// <summary>
        /// shutdown service application
        /// </summary>
        void Shutdown();
    }

    internal class RpcServiceAdministration : RpcService, IRpcServiceAdministration
    {
        public IRpcServer Server { get; internal set; }
        [RpcServiceAdminAuthorize]
        public void Shutdown()
        {
            Server.Stop();
        }

        public void Touch()
        {
            //nothing to do and just process a request with empty response to warmup connection;
        }

        public override RpcIdentity Authroize(string token)
        {
            return base.Authroize(token);
        }
    }
}
