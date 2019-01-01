using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class ReflectionHelper
    {
        public static MethodBase ResolveMethod(Type type, int mdtoken)
        {
            try
            {
                return type.Assembly.ManifestModule.ResolveMethod(mdtoken);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static IEnumerable<RpcServiceMethod> GetRpcServiceInfo(IEnumerable<object> impls)
        {
            foreach (object impl in impls)
            {
                if (impl == null)
                    continue;
                var implType = impl.GetType();
                var interfaces = implType.GetInterfaces();
                foreach (var itf in interfaces)
                {
                    InterfaceMapping map = implType.GetInterfaceMap(itf);
                    MethodInfo[] interfaceMethods = map.InterfaceMethods;
                    if (interfaceMethods.Length > 0)
                    {
                        MethodInfo[] implementationMethods = map.TargetMethods;
                        Console.WriteLine("interface={0}[{1}] type_impl={2}[{3}]", itf.FullName, itf.MetadataToken, implType.Name, implType.MetadataToken);
                        for (int i = 0; i < interfaceMethods.Length; i++)
                        {
                            var m = new RpcServiceMethod();
                            m.Assembly = itf.Assembly.FullName;
                            m.Namespace = itf.Namespace;
                            m.MdToken = itf.MetadataToken;
                            m.DeclareType = itf.Name;
                            m.Name = interfaceMethods[i].Name;
                            yield return m;
                        }
                    }
                }
            }
        }
    }
}
