using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.Serialization
{
    /// <summary>
    /// 请求META信息序列化接口，用于序列化和反序列化请求头
    /// </summary>
    public interface IRpcHeadSerializer
    {
        string Serialize(RpcHead head);
        RpcHead Deserialize(string value);
    }

    /// <summary>
    /// IRpcHeadSerializer内置默认实现
    /// </summary>
    public class JsonRpcHeadSerializer : IRpcHeadSerializer
    {
        public RpcHead Deserialize(string value)
        {
            return JObject.Parse(value).ToObject<RpcHead>();
        }

        public string Serialize(RpcHead head)
        {
            return JObject.FromObject(head).ToString(Formatting.None);
        }
    }

    /// <summary>
    /// 数据序列化接口，用于序列化和反序列化请求参数
    /// </summary>
    public interface IRpcDataSerializer
    {
        void Serialize(Stream writeStream, Type[] types, object[] args);
        object[] Deserialize(Stream readStream, Type[] types);
    }

    /// <summary>
    /// IRpcDataSerializer的内置默认实现
    /// </summary>
    public partial class ProtoBufRpcDataSerializer : IRpcDataSerializer
    {
        public object[] Deserialize(Stream readStream, Type[] types)
        {
            var reader = new BinaryReader(readStream);
            var argLenth = reader.ReadByte();
            var args = new object[argLenth];
            for (int i = 0; i < argLenth; i++)
            {
                var itemType = types[i];
                if (typeof(Stream).IsAssignableFrom(itemType))
                {
                    //TODO: large stream write to temp file, small size stream write to memory stream
                    var tempFs = new TempFileStream(Path.GetTempFileName(), FileMode.Create);
                    int loop = 0;
                    while (true)
                    {
                        var buff = (byte[])RuntimeTypeModel.Default.DeserializeWithLengthPrefix(readStream, null, typeof(byte[]), ProtoBuf.PrefixStyle.Base128, args.Length + i * loop);
                        if (buff.Length == 0)
                        {
                            tempFs.Position = 0;
                            break;
                        }
                        tempFs.Write(buff, 0, buff.Length);
                        loop++;
                    }
                    args[i] = tempFs;
                }
                else
                    args[i] = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(readStream, null, itemType, ProtoBuf.PrefixStyle.Base128, i);
            }
            return args;
        }

        public void Serialize(Stream writeStream, Type[] types, object[] args)
        {
            BinaryWriter writer = new BinaryWriter(writeStream);
            if (args.Length > byte.MaxValue)
            {
                throw new IndexOutOfRangeException("the number of method arguments is limited to 255 max");
            }
            writer.Write((byte)args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                var item = args[i];
                var itemType = types[i];
                if (typeof(Stream).IsAssignableFrom(itemType))
                {
                    var streamArg = (item as Stream);
                    var buff = new byte[1024];
                    int length = 0;
                    int loop = 0;
                    while ((length = streamArg.Read(buff, 0, buff.Length)) > 0)
                    {
                        if (length < buff.Length)
                        {
                            Array.Resize(ref buff, length);
                        }
                        RuntimeTypeModel.Default.SerializeWithLengthPrefix(writeStream, buff, typeof(byte[]), ProtoBuf.PrefixStyle.Base128, args.Length + i * loop);
                        loop++;
                    }
                    //flag end
                    RuntimeTypeModel.Default.SerializeWithLengthPrefix(writeStream, new byte[0], typeof(byte[]), ProtoBuf.PrefixStyle.Base128, args.Length + i * loop);
                }
                else
                {
                    try
                    {
                        if (item != null)
                        {
                            RuntimeTypeModel.Default.SerializeWithLengthPrefix(writeStream, item, itemType, ProtoBuf.PrefixStyle.Base128, i);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
