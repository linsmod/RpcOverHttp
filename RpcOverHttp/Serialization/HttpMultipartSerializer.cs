using HttpMultipartParser;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RpcOverHttp.Serialization
{
    class HttpMultipartSerializer : IRpcDataSerializer, IRpcDataSerializer2
    {
        public object[] Deserialize(Stream readStream, Type[] types)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Stream writeStream, Type[] types, object[] args)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Stream writeStream, Type[] types, object[] args, string[] names)
        {
            string boundary = "----RpcOverHttp" + DateTime.Now.Ticks.ToString("x");
            //request.ContentType = "multipart/form-data; boundary=" + boundary;
            //request.Method = "POST";

            using (var writer = new StreamWriter(writeStream, Encoding.UTF8))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (arg == null)
                        continue;
                    writer.WriteLine("--" + boundary);
                    writer.WriteLine(string.Format("Content-Disposition: form-data; name=\"{0}\"", names[i]));
                    writer.WriteLine();
                    if (typeof(Stream).IsAssignableFrom(types[i]))
                    {
                        (arg as Stream).CopyTo(writeStream);
                    }
                    else if (simpleTypes.Any(x => x == types[i]))
                    {
                        writer.WriteLine(arg);
                    }
                    else if (types[i].IsClass && !types[i].IsAbstract && types[i].IsPublic)
                    {
                        var properties = types[i].GetProperties().Where(x => x.CanRead && x.CanWrite);
                        SerializeInternal(writer,
                            properties.Select(x => x.PropertyType).ToArray(),
                            properties.Select(x => x.GetValue(args[i])).ToArray(),
                            properties.Select(x => x.Name).ToArray());
                    }
                    else
                    {
                        throw new NotSupportedException(string.Format("parameter at {0} using type {1} is not supported by HttpMultipartSerializer", i, types[i]));
                    }
                }
                writer.WriteLine(boundary + "--");
            }
        }
        void SerializeInternal(StreamWriter writer, Type[] types, object[] args, string[] names)
        {
            string boundary = "----RpcOverHttp" + DateTime.Now.Ticks.ToString("x");
            //request.ContentType = "multipart/form-data; boundary=" + boundary;
            //request.Method = "POST";

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg == null)
                    continue;
                writer.WriteLine("--" + boundary);
                writer.WriteLine(string.Format("Content-Disposition: form-data; name=\"{0}\"", names[i]));
                writer.WriteLine();
                if (typeof(Stream).IsAssignableFrom(types[i]))
                {
                    (arg as Stream).CopyTo(writer.BaseStream);
                }
                else if (simpleTypes.Any(x => x == types[i]))
                {
                    writer.WriteLine(arg);
                }
                else
                {
                    throw new NotSupportedException(string.Format("property \"{0}\" using type {1} is not supported by HttpMultipartSerializer", names[i], types[i]));
                }
            }
            writer.WriteLine(boundary + "--");
        }

        public object[] Deserialize(Stream readStream, Type[] types, string[] names)
        {
            var retVal = new object[types.Length];
            var parser = new HttpMultipartParser.MultipartFormDataParser(readStream);
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (simpleTypes.Any(x => x == types[i]))
                {
                    var part = parser.BodyParts.FirstOrDefault(x => x.Name.Equals(names[i], StringComparison.OrdinalIgnoreCase));
                    if (part == null)
                    {
                        retVal[i] = null;
                    }
                    else
                    {
                        var token = Newtonsoft.Json.Linq.JToken.Parse((part as ParameterPart).Data);
                        retVal[i] = token.ToObject(type);
                    }
                }
                else if (typeof(Stream).IsAssignableFrom(type))
                {
                    var part = parser.BodyParts.FirstOrDefault(x => x.Name.Equals(names[i], StringComparison.OrdinalIgnoreCase));
                    if (part == null)
                    {
                        retVal[i] = null;
                    }
                    else
                    {
                        retVal[i] = (part as FilePart).Data;
                    }
                }
                else if (type.IsClass && type.IsPublic && !type.IsAbstract)
                {
                    //class model
                    try
                    {
                        var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite).ToArray();
                        var obj = Activator.CreateInstance(type);
                        var values = DeserializeInternal(parser, properties.Select(x => x.PropertyType).ToArray(), properties.Select(x => x.Name).ToArray());
                        for (int idx = 0; idx < properties.Length; idx++)
                        {
                            properties[idx].SetValue(obj, values[i]);
                        }
                        retVal[i] = obj;
                    }
                    catch (Exception ex)
                    {
                        throw new NotSupportedException(string.Format("parameter at {0} using type {1} is not supported by HttpMultipartSerializer", i, types[i]), ex);
                    }
                }
                else
                {
                    throw new NotSupportedException(string.Format("parameter at {0} using type {1} is not supported by HttpMultipartSerializer", i, types[i]));
                }
            }
            return retVal;
        }
        object[] DeserializeInternal(MultipartFormDataParser parser, Type[] types, string[] names)
        {
            var retVal = new object[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (simpleTypes.Any(x => x == types[i]))
                {
                    var part = parser.BodyParts.FirstOrDefault(x => x.Name.Equals(names[i], StringComparison.OrdinalIgnoreCase));
                    if (part == null)
                    {
                        retVal[i] = null;
                    }
                    else
                    {
                        var token = Newtonsoft.Json.Linq.JToken.Parse((part as ParameterPart).Data);
                        retVal[i] = token.ToObject(type);
                    }
                }
                else if (typeof(Stream).IsAssignableFrom(type))
                {
                    var part = parser.BodyParts.FirstOrDefault(x => x.Name.Equals(names[i], StringComparison.OrdinalIgnoreCase));
                    if (part == null)
                    {
                        retVal[i] = null;
                    }
                    else
                    {
                        retVal[i] = (part as FilePart).Data;
                    }
                }
                else
                {
                    throw new NotSupportedException(string.Format("property \"{0}\" on type {1} is not supported by HttpMultipartSerializer", names[i], types[i]));
                }
            }
            return retVal;
        }

        private static Type[] simpleTypes = new Type[] {
            typeof(string),
            typeof(Int64),
            typeof(Int32),
            typeof(Int16),
            typeof(UInt64),
            typeof(UInt32),
            typeof(UInt16),
            typeof(Single),
            typeof(Decimal),
            typeof(Double),
            typeof(Byte),
            typeof(SByte)
        };
    }
}
