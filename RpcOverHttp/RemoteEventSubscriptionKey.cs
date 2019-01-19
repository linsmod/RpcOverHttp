using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RemoteEventSubscription : IEqualityComparer<RemoteEventSubscription>
    {
        public RemoteEventSubscription(Guid instanceId, Type interfaceType, string eventName, MethodInfo handlerMethod)
        {
            this.InstanceId = instanceId;
            this.InterfaceType = interfaceType;
            this.EventName = eventName;
            this.HandlerMethod = handlerMethod;
        }

        public Guid InstanceId { get; private set; }
        public Type InterfaceType { get; private set; }
        public string EventName { get; private set; }
        public MethodInfo HandlerMethod { get; private set; }
        public bool Equals(RemoteEventSubscription x, RemoteEventSubscription y)
        {
            return x.InstanceId.Equals(y.InstanceId)
                && x.InterfaceType.Equals(y.InterfaceType)
                && x.EventName.Equals(y.EventName)
                && x.HandlerMethod.Equals(y.HandlerMethod);
        }

        public int GetHashCode(RemoteEventSubscription obj)
        {
            return (string.Join("|",
                obj.InstanceId,
                obj.InterfaceType.Assembly.FullName,
                obj.InterfaceType.FullName,
                EventName,
                HandlerMethod.DeclaringType.Assembly.FullName,
                HandlerMethod.DeclaringType.FullName,
                HandlerMethod.Name,
                HandlerMethod.MetadataToken
                )).GetHashCode();
        }
        public static implicit operator int(RemoteEventSubscription key)
        {
            return key.GetHashCode();
        }
    }
}
