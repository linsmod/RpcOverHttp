using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public class RpcUser
    {
        public string Name { get;  set; }
        public string[] Roles { get;  set; }

        public RpcIdentity ToIdentity()
        {
            return new RpcIdentity(this.Name, this.Roles);
        }
        public static RpcUser FromIdentity(RpcIdentity identity)
        {
            return new RpcUser { Name = identity.Name, Roles = identity.Roles };
        }
    }
}
