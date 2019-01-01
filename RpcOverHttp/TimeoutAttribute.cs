using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    /// <summary>
    /// for method executing timeout control
    /// <para>if non control, the default timeout is 120s for a method call.</para>
    /// </summary>
    public class TimeoutAttribute : Attribute
    {
        public int Milliseconds { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="milliseconds"></param>
        public TimeoutAttribute(int milliseconds)
        {
            this.Milliseconds = milliseconds;
        }
    }
}
