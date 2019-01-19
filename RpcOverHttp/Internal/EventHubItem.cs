using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.Internal
{
    class EventHubItem
    {
        public List<int> callbackIds { get; set; }
        public Delegate d { get; set; }
    }
}
