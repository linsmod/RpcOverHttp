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
        public RemoteEventSubscriptionKey(Type interfaceType, string eventName, MethodInfo handlerMethod)
        {
            this.InterfaceType = interfaceType;
            this.EventName = eventName;
            this.HandlerMethod = handlerMethod;
        }

        public Type InterfaceType { get; set; }
        public string EventName { get; set; }
        public MethodInfo HandlerMethod { get; set; }
        public bool Equals(RemoteEventSubscriptionKey x, RemoteEventSubscriptionKey y)
        {
            return x.InterfaceType.Equals(y.InterfaceType)
                && x.EventName.Equals(y.EventName)
                && x.HandlerMethod.Equals(y.HandlerMethod);
        }

        public int GetHashCode(RemoteEventSubscriptionKey obj)
        {
            return (string.Join("|", obj.InterfaceType.Assembly.FullName,
                obj.InterfaceType.FullName,
                EventName,
                HandlerMethod.DeclaringType.Assembly.FullName,
                HandlerMethod.DeclaringType.FullName,
                HandlerMethod.Name,
                HandlerMethod.MetadataToken
                )).GetHashCode();
        }
        public static implicit operator int(RemoteEventSubscriptionKey key)
        {
            return key.GetHashCode();
        }
    }
}
