using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TinyIoC;

namespace RpcOverHttp.Internal
{
    class ClientInfo
    {
        /// <summary>
        /// the client's id
        /// </summary>
        public string clientId { get; set; }
    }

    class ClientWebSocketInfo : ClientInfo
    {
        /// <summary>
        /// the client websocket id
        /// </summary>
        public string websocketId { get; set; }
    }

    class ClientEventHandlerInfo : ClientWebSocketInfo
    {
        /// <summary>
        /// the service impl's instance id
        /// </summary>
        public Guid InstanceId { get; set; }
        /// <summary>
        /// the delegate instance id
        /// </summary>
        public int handlerId { get; set; }

        internal static ThreadLocal<ClientEventHandlerInfo> _provider;
        public static ClientEventHandlerInfo Current
        {
            get
            {
                return _provider == null ? null : _provider.Value;
            }
        }
        public static void SetCurrent(ClientEventHandlerInfo value)
        {
            _provider = new ThreadLocal<ClientEventHandlerInfo>();
            _provider.Value = value;
        }
    }
}
