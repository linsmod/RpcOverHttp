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
        public RemoteEventSubscription(Guid instanceId, Type interfaceType, string eventName, Delegate d)
        {
            this.InstanceId = instanceId;
            this.InterfaceType = interfaceType;
            this.EventName = eventName;
            this.Handler = d;
        }

        public Guid InstanceId { get; private set; }
        public Type InterfaceType { get; private set; }
        public string EventName { get; private set; }
        public Delegate Handler { get; private set; }
        public bool Equals(RemoteEventSubscription x, RemoteEventSubscription y)
        {
            return x.InstanceId.Equals(y.InstanceId)
                && x.InterfaceType.Equals(y.InterfaceType)
                && x.EventName.Equals(y.EventName)
                && x.Handler.Equals(y.Handler);
        }

        public int GetHashCode(RemoteEventSubscription obj)
        {
            return (string.Join("|",
                obj.InstanceId,
                obj.InterfaceType.Assembly.FullName,
                obj.InterfaceType.FullName,
                EventName,
                Handler.Target.GetType().Assembly.FullName,
                Handler.Target.GetType().FullName,
                Handler.Method.DeclaringType.Assembly.FullName,
                Handler.Method.DeclaringType.FullName,
                Handler.Method.Name,
                Handler.Method.MetadataToken
                )).GetHashCode();
        }
        public static implicit operator int(RemoteEventSubscription key)
        {
            return key.GetHashCode();
        }
    }
}
