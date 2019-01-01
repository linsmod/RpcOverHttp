using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class JsonHelper
    {
        public static string ToString<T>(T value)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.None);
        }

        public static object FromString(string value, Type type)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(value, type);
        }
        public static T FromString<T>(string value)
        {
            try
            {
                return (T)Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
            }
            catch
            {
                return default(T);
            }
        }

        public static T FromFile<T>(string path)
        {
            var text = File.ReadAllText(path);
            return FromString<T>(text);
        }

        internal static void ToFile<T>(string path, T value)
        {
            var text = ToString<T>(value);
            File.WriteAllText(path, text);
        }
    }
}
