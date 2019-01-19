using DynamicProxyImplementation;
using RpcOverHttp.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.Server
{
    internal class ThunkImplementationFactory
    {
        DynamicProxyFactory<ThunkImplementation> factory = new DynamicProxyFactory<ThunkImplementation>(new DynamicInterfaceImplementor());
        public TInterface GetProxy<TInterface>(object instance)
        {
            return factory.CreateDynamicProxy<TInterface>(this, instance);
        }

        public object GetProxy(Type interfaceType, object instance, Guid instanceId, RpcServer server)
        {
            return factory.CreateDynamicProxy(interfaceType, this, instance, instanceId, server);
        }
    }
    internal class ThunkImplementation : DynamicProxy, IRpcService
    {
        private ThunkImplementationFactory factory;
        private object instance;
        private RpcServer server;
        private Guid instanceId;

        RpcPrincipal IRpcService.User
        {
            get
            {
                var service = this.instance as IRpcService;
                if (service != null)
                {
                    return service.User;
                }
                return null;
            }
        }

        public ThunkImplementation(ThunkImplementationFactory factory, object instance, Guid instanceId, RpcServer server)
        {
            this.factory = factory;
            this.instance = instance;
            this.server = server;
            this.instanceId = instanceId;
        }
        protected override bool TryGetMember(Type interfaceType, string name, out object result)
        {
            throw new NotSupportedException("rpc interface is not support memeber access.");
        }

        protected override bool TrySetMember(Type interfaceType, string name, object value)
        {
            throw new NotSupportedException("rpc interface is not support memeber access.");
        }

        protected override bool TryInvokeMember(Type interfaceType, int id, bool eventOp, object[] args, out object result)
        {
            var method = interfaceType.Assembly.ManifestModule.ResolveMethod(id) as MethodInfo;
            result = method.Invoke(this.instance, args);
            return true;
        }

        protected override bool TrySetEvent(Type interfaceType, string name, object value, bool add)
        {
            object result;
            EventInfo e = TypeHelper.GetEventInfo(interfaceType, name);
            var m = add ? e.AddMethod : e.RemoveMethod;
            result = m.Invoke(this.instance, new object[] { value });
            return true;
        }



        protected override bool TryInvokeEventHandler(Type interfaceType, Type handlerType, string name, object[] args, out object result)
        {
            var method = handlerType.GetMethod("Invoke");
            RpcEvent rpcEvent = new RpcEvent();
            rpcEvent.Arguments = args;
            rpcEvent.ArgumentTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
            rpcEvent.ReturnType = method.ReturnType;

            //control only invoking the matched event hander registeration
            if (RpcHead.Current.InstanceId != this.instanceId)
            {
                result = null;
                return false;
            }
            if (!EventHub.hanlderMap.TryGetValue(RpcHead.Current.InstanceId + "." + name, out EventHubItem item))
            {
                throw new Exception("can not found the client handler.");
            }
            rpcEvent.handlerId = item.callbackIds[0];
            //rpcEvent.handlerId = item.callbackIds[0];
            BlockingQueue<RpcEvent> queue;
            server.eventMessages.TryGetValue(RpcHead.Current.InstanceId, out queue);

            queue.Enqueue(rpcEvent);
            var invokeResult = rpcEvent.WaitResult(120 * 1000);
            if (invokeResult.Error != null)
            {
                Console.WriteLine("client feed is a error object.");
                throw new RpcException(string.Format("the client failed to handle event {0}", rpcEvent.handlerId), invokeResult.Error, RpcErrorLocation.ClientEventHandler);
            }
            else
            {
                Console.WriteLine("client feed is a valid result.");
                if (method.ReturnType == typeof(void))
                {
                    result = null;
                }
                else
                {
                    result = invokeResult.Value;
                }
                return true;
            }
        }

        RpcIdentity IRpcService.Authroize(string token)
        {
            var service = this.instance as IRpcService;
            if (service != null)
            {
                return service.Authroize(token);
            }
            else
            {
                return server.AuthroizeHandler.Authroize(token);
            }
        }

        RpcError IRpcService.HandleException(RpcHead head, Exception ex)
        {
            var service = this.instance as IRpcService;
            if (service != null)
            {
                return service.HandleException(head, ex);
            }
            else
            {
                return server.ExceptionHandler.HandleException(head, ex);
            }
        }
    }
}
