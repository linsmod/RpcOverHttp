using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class Certificate
    {
        public byte[] Hash { get; set; }
        public string Thumbprint { get; set; }
        public string Name { get; set; }
    }
}
