using Newtonsoft.Json.Linq;
using RpcServiceCollection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RpcService.Implementation
{
    public class AntiVCodeImpl : IAntiVCode
    {
        public string sina_weibo_image2pin(byte[] img)
        {
            try
            {
                if (img != null && img.Length > 0)
                {
                    WebClient c = new WebClient();
                    var ret = c.UploadString("http://39.98.63.101", Convert.ToBase64String(img));
                    Console.WriteLine("http://39.98.63.101 returns " + ret);
                    if (!string.IsNullOrEmpty(ret))
                    {
                        return JObject.Parse(ret)["code"].ToString();
                    }
                    return ret;
                }
                else
                {
                    return "ERROR_INVALID_IMAGE";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "ERROR_INTERNAL_EX";
            }
        }
    }
}
