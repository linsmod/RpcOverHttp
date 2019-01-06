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
    internal class CommonEventHandler
    {
        static List<MethodInfo> actions = new List<MethodInfo>();
        static List<MethodInfo> funcs = new List<MethodInfo>();
        static CommonEventHandler()
        {
            var methods = typeof(CommonEventHandler).GetMethods();
            foreach (var item in methods)
            {
                //action
                if (item.ReturnType == typeof(void))
                {
                    actions.Add(item);
                }
                else
                {
                    //func
                    funcs.Add(item);
                }
            }
        }
        public CommonEventHandler(RpcServer server, Guid instanceId)
        {
            this.server = server;
            this.InstanceId = instanceId;
        }

        private RpcServer server;
        public Guid InstanceId { get; private set; }
        public void Invoke()
        {

        }
        public void Invoke<T>(T arg0)
        {

        }

        public void Invoke<T, T1>(T arg0, T1 arg1)
        {

        }

        public void Invoke<T, T1, T2>(T arg0, T1 arg1, T2 arg2)
        {

        }
        public void Invoke<T, T1, T2, T3>(T arg0, T1 arg1, T2 arg2, T3 arg3)
        {

        }

        public void Invoke<T, T1, T2, T3, T4>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {

        }

        public void Invoke<T, T1, T2, T3, T4, T5>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {

        }

        public void Invoke<T, T1, T2, T3, T4, T5, T6>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {

        }

        public void Invoke<T, T1, T2, T3, T4, T5, T6, T7>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {

        }

        public TResult Invoke<TResult>()
        {
            throw new NotImplementedException();
        }
        public TResult Invoke<T, TResult>(T arg0)
        {
            var returnType = typeof(TResult);
            var argumentTypes = new Type[] { typeof(T) };

            var key = this.subscriptions.Keys
                .FirstOrDefault(x => x.ReturnType == returnType && x.GetParameters().Select(item => item.ParameterType)
                .SequenceEqual(argumentTypes));
            if (key != null)
            {
                if (this.subscriptions.TryGetValue(key, out int subscriptionKey))
                {
                    return (TResult)this.InvokeSubscription(subscriptionKey, argumentTypes, new object[] { arg0 }, typeof(TResult));
                }
            }
            throw new Exception("client subscription is removed.");
        }

        public TResult Invoke<T, T1, TResult>(T arg0, T1 arg1)
        {
            throw new NotImplementedException();
        }

        public TResult Invoke<T, T1, T2, TResult>(T arg0, T1 arg1, T2 arg2)
        {
            throw new NotImplementedException();
        }
        public TResult Invoke<T, T1, T2, T3, TResult>(T arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            throw new NotImplementedException();
        }

        public TResult Invoke<T, T1, T2, T3, T4, TResult>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            throw new NotImplementedException();
        }

        public TResult Invoke<T, T1, T2, T3, T4, T5, TResult>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            throw new NotImplementedException();
        }

        public TResult Invoke<T, T1, T2, T3, T4, T5, T6, TResult>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            throw new NotImplementedException();
        }

        public TResult Invoke<T, T1, T2, T3, T4, T5, T6, T7, TResult>(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            throw new NotImplementedException();
        }

        private SafeDictionary<MethodInfo, int> subscriptions = new SafeDictionary<MethodInfo, int>();


        private object InvokeSubscription(int subscriptionId, Type[] argumentTypes, object[] args, Type returnType)
        {
            RpcEvent rpcEvent = new RpcEvent();
            rpcEvent.EventKey = subscriptionId;
            rpcEvent.Arguments = args;
            rpcEvent.ArgumentTypes = argumentTypes;
            rpcEvent.ReturnType = returnType;
            Queue<RpcEvent> queue;
            server.eventMessages.TryGetValue(this.InstanceId, out queue);
            queue.Enqueue(rpcEvent);
            var result = rpcEvent.WaitResult();
            if (result.Error != null)
            {
                throw new RpcException("the client failed to handle event {0}", result.Error, RpcErrorLocation.ClientEventHandler);
            }
            else
                return result.Value;
        }

        /// <summary>
        /// create a delegate against the event handler
        /// </summary>
        /// <param name="eventHanderType"></param>
        /// <param name="m_signature"></param>
        /// <returns></returns>
        public Delegate CreateProxyDelegate(Type eventHanderType, MethodInfo m_signature, int eventSubscriptionId)
        {
            if (subscriptions.TryGetValue(m_signature, out int id))
            {
                throw new InvalidOperationException("only allowed 1 event handler be attached to a event");
            }
            subscriptions[m_signature] = eventSubscriptionId;
            var d = Delegate.CreateDelegate(eventHanderType, this, FindByMethod(m_signature));
            return d;
        }

        /// <summary>
        /// create a handler method against the event handler method
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        static MethodInfo FindByMethod(MethodInfo m)
        {
            try
            {
                MethodInfo invoker;
                var parameterTypes = m.GetParameters().Select(x => x.ParameterType).ToArray();
                if (m.ReturnType == typeof(void))
                {
                    invoker = actions.Single(x => x.Name == m.Name && x.GetParameters().Count() == m.GetParameters().Count());
                    return invoker.MakeGenericMethod(parameterTypes);
                }
                else
                {
                    invoker = funcs.Single(x => x.Name == m.Name && x.GetParameters().Count() == m.GetParameters().Count());
                    return invoker.MakeGenericMethod(parameterTypes.Concat(new Type[] { m.ReturnType }).ToArray());
                }
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("the event is not supported by rpc server may because of too many arguments.");
            }
        }
    }
}
