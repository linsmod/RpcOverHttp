using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public class RpcIdentity : IIdentity
    {
        /// <summary>
        /// ctor only for json serialize/deserialize
        /// </summary>
        public RpcIdentity() { }
        public string[] Roles { get; set; }
        public RpcIdentity(string name, string[] roles)
        {
            this.Name = name;
            this.Roles = roles;
        }
        public string AuthenticationType
        {
            get
            {
                return "Token";
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return Name != null && Name!= "Anonymous";
            }
        }

        public string Name { get; set; }
    }
}
