using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RpcServiceMethod
    {
        public int MdToken { get; set; }
        public string Assembly { get; set; }
        public string DeclareType { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
    }
}
