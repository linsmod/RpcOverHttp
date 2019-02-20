using Newtonsoft.Json;
using System;
using System.Threading;

namespace RpcOverHttp
{
    /// <summary>
    /// 请求元数据
    /// </summary>
    public class RpcHead
    {
        /// <summary>
        /// 请求ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 通过instanceId查找实例，找不到就创建一个
        /// </summary>
        public Guid InstanceId { get; set; }

        /// <summary>
        /// 调用key
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// method mdtoken of the itf
        /// </summary>
        public int MethodMDToken { get; set; }

        /// <summary>
        /// 接口方法名称
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 接口类型名称
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 接口命名空间
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// 是否事件注册/反注册
        /// </summary>
        public bool EventOp { get; set; }

        /// <summary>
        /// 客户端超时设置，服务端根据该设置值调整Task类型任务的执行时间
        /// </summary>
        public int Timeout { get; set; }

        internal RpcEventOp GetEventOp()
        {
            if (!this.EventOp)
            {
                throw new NotSupportedException("request must be a event operation.");
            }
            var p = this.MethodName.Split(new char[] { '_' }, 2);
            return new RpcEventOp
            {
                EventKind = p[0] == "add" ? RpcEventKind.Add : RpcEventKind.Remove,
                EventName = p[1]
            };
        }
        internal static ThreadLocal<RpcHead> _provider = new ThreadLocal<RpcHead>();
        public static RpcHead Current
        {
            get
            {
                return _provider == null ? null : _provider.Value;
            }
        }
        public static void SetCurrent(RpcHead value)
        {
            _provider.Value = value;
        }
    }

    /// <summary>
    /// represent the result to the client handler method for a event handler
    /// </summary>
    [ProtoBuf.ProtoContract]
    internal class RpcEventHandleResultGeneral<T> : IRpcEventHandleResult
    {
        [ProtoBuf.ProtoMember(1)]
        public RpcError Error { get; set; }
        [ProtoBuf.ProtoMember(2)]
        public T Value { get; set; }
        [ProtoBuf.ProtoIgnore]
        RpcError IRpcEventHandleResult.Error
        {
            get
            {
                return this.Error;
            }
            set
            {
                this.Error = value;
            }
        }
        [ProtoBuf.ProtoIgnore]
        object IRpcEventHandleResult.Value
        {
            get
            {
                return this.Value;
            }
            set
            {
                this.Value = (T)value;
            }
        }
    }

    internal interface IRpcEventHandleResult
    {
        RpcError Error { get; set; }
        object Value { get; set; }
    }
    [ProtoBuf.ProtoContract]
    internal class RpcEventHandleResultVoid : IRpcEventHandleResult
    {
        [ProtoBuf.ProtoMember(1)]
        public RpcError Error { get; set; }
        [ProtoBuf.ProtoIgnore]
        RpcError IRpcEventHandleResult.Error
        {
            get
            {
                return this.Error;
            }
            set
            {
                this.Error = value;
            }
        }
        [ProtoBuf.ProtoIgnore]
        object IRpcEventHandleResult.Value
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                if (value != null)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }

    internal class RpcEventData
    {
        public int EventKey { get; internal set; }
        public object[] Arguments { get; internal set; }
    }

    /// <summary>
    /// which is a event will send to client to handle.
    /// </summary>
    internal class RpcEvent
    {
        public static RpcEvent Empty = new RpcEvent(true);
        public RpcEvent() : this(false) { }
        private RpcEvent(bool empty)
        {
            if (!empty)
                waitHandle = new AutoResetEvent(false);
        }
        public Type[] ArgumentTypes { get; internal set; }
        public object[] Arguments { get; internal set; }
        public Type ReturnType { get; internal set; }
        AutoResetEvent waitHandle = new AutoResetEvent(false);
        IRpcEventHandleResult Result { get; set; }
        public int handlerId { get; internal set; }

        internal void SetResult(IRpcEventHandleResult result)
        {
            this.Result = result;
            waitHandle.Set();
        }
        internal IRpcEventHandleResult WaitResult(int millisecondsTimeout)
        {
            var waits = 12;
            if (millisecondsTimeout > 1000)
            {
                waits = (int)Math.Ceiling(millisecondsTimeout / 1000.0);
            }
            else
            {
                waits = 1;
            }
            while (waits > 0)
            {
                if (!waitHandle.WaitOne(1000))
                {
                    Console.WriteLine("a server thread is waiting client feed for handler {0}...", handlerId);
                    waits--;
                }
                else
                {
                    Console.WriteLine("a client feed received.");
                    return this.Result;
                }
            }
            Console.WriteLine("server is failed to continue as wsrpc timeout.");
            throw new TimeoutException(string.Format("rpc to client is timeout, subscription key is {0}.", handlerId));
        }
    }

    internal class RpcEventOp
    {
        internal RpcEventKind EventKind { get; set; }
        internal string EventName { get; set; }

    }
    internal enum RpcEventKind
    {
        Add,
        Remove
    }

    internal class RpcRequest : RpcHead
    {
        /// <summary>
        /// 参数
        /// </summary>
        [JsonIgnore]
        public object[] Arguments { get; set; }
    }
}
