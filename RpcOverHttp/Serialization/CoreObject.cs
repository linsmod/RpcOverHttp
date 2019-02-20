using System;
using System.IO;

namespace RpcOverHttp.Serialization
{
    public class CoreObject
    {
        static ProtoBufRpcDataSerializer serializer = new ProtoBufRpcDataSerializer();
        public static object Deserialize(Type type, Stream valueStream)
        {
            return serializer.Deserialize(valueStream, new Type[] { type }, new string[] { "" })[0];
        }

        public static void Serialize(Stream writeStream, Type type, object val, string name)
        {
            serializer.Serialize(writeStream, new Type[] { type }, new object[] { val }, new string[] { name });
        }
    }
}
