using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    }

    internal class RpcRequest : RpcHead
    {
        /// <summary>
        /// 参数
        /// </summary>
        public object[] Arguments { get; set; }
    }
}
