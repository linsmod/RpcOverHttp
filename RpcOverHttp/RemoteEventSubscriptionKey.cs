using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RemoteEventSubscriptionKey : IEqualityComparer<RemoteEventSubscriptionKey>
    {
        public RemoteEventSubscriptionKey(MethodInfo interfaceMethod, MethodInfo eventHandlerMethod)
        {
            InterfaceMethod = interfaceMethod;
            EventHandlerMethod = eventHandlerMethod;
        }

        public MethodInfo InterfaceMethod { get; set; }
        public MethodInfo EventHandlerMethod { get; set; }

        public bool Equals(RemoteEventSubscriptionKey x, RemoteEventSubscriptionKey y)
        {
            return x.InterfaceMethod.Equals(y.InterfaceMethod) && x.EventHandlerMethod.Equals(y.EventHandlerMethod);
        }

        public int GetHashCode(RemoteEventSubscriptionKey obj)
        {
            return (obj.InterfaceMethod.DeclaringType.FullName + obj.InterfaceMethod.MetadataToken + obj.EventHandlerMethod.DeclaringType.FullName + obj.EventHandlerMethod.MetadataToken).GetHashCode();
        }
    }
}
