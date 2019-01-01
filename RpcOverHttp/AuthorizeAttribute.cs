using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public class AuthorizeAttribute : Attribute
    {
        public IPrincipal User
        {
            get
            {
                return Thread.CurrentPrincipal as RpcPrincipal;
            }
        }
        public AuthorizeAttribute()
        {

        }
        public virtual bool IsAuthroized()
        {
            return User.Identity.IsAuthenticated;
        }
    }
}
