using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public class RpcPrincipal : IPrincipal
    {
        public RpcPrincipal(RpcIdentity id)
        {
            this.Identity = id;
        }
        public IIdentity Identity { get; set; }

        public bool IsInRole(string role)
        {
            return ((RpcIdentity)Identity).Roles.Any(x => x == role);
        }
    }
}
