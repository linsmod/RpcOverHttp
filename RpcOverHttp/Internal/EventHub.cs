using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TinyIoC;

namespace RpcOverHttp.Internal
{
    internal class EventHub
    {
        internal static SafeDictionary<string, EventHubItem> hanlderMap = new SafeDictionary<string, EventHubItem>();

        internal RpcServer server;
        public EventHub(RpcServer server)
        {
            this.server = server;
        }

        internal static void AddEventHandler(EventInfo e, object impl, Guid instanceId, MethodInfo thunkHanlder, int clientHandlerId)
        {
            EventHubItem hubItem;
            var key = instanceId + "." + thunkHanlder.Name;
            if (!hanlderMap.TryGetValue(key, out hubItem))
            {
                var d = Delegate.CreateDelegate(e.EventHandlerType, impl, thunkHanlder);
                e.AddEventHandler(impl, d);
                hanlderMap[key] = new EventHubItem
                {
                    callbackIds = new List<int> { clientHandlerId },
                    d = d
                };
            }
            else
            {
                throw new Exception("the subscription to a event can only be add once. unsubscribe first if resubscribe.");
                //hubItem.callbackIds.Add(clientHandlerId);
            }
        }

        internal static void RemoveEventHandler(EventInfo e, object impl, Guid instanceId, MethodInfo thunkHanlder, int clientHandlerId)
        {
            EventHubItem hubItem;
            //仅当映射移除完毕后
            var key = instanceId + "." + thunkHanlder.Name;
            if (hanlderMap.TryGetValue(key, out hubItem))
            {
                hubItem.callbackIds.Remove(clientHandlerId);
                if (!hubItem.callbackIds.Any())
                {
                    e.RemoveEventHandler(impl, hubItem.d);
                    hanlderMap.Remove(key);
                }
            }
        }
    }
}
