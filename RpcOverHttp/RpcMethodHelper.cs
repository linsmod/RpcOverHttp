using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal class RpcMethodHelper
    {
        public static bool IsAuthoirzied(Type itfType, MethodInfo itfMethod, Type instanceType)
        {
            MethodInfo implMethod = FindImplMethod(itfType, itfMethod, instanceType);
            //检查权限
            var implAttrs = implMethod.GetCustomAttributes(true);
            var auths = instanceType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Concat(implMethod.GetCustomAttributes(true).OfType<AuthorizeAttribute>());
            bool implMethodAuthRequired = auths.Any();
            if (implMethodAuthRequired)
            {
                if (auths.Any(x => !x.IsAuthroized()))
                {
                    return false;
                }
                else if (auths.All(x => x.IsAuthroized()))
                {
                    return true;
                }
                else
                {
                    var skipAuths = implMethod.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>();
                    return implMethodAuthRequired && !skipAuths.Any() && !Thread.CurrentPrincipal.Identity.IsAuthenticated;
                }
            }
            return true;
        }
        public static MethodInfo FindImplMethod(Type itfType, MethodInfo itfMethod, Type instanceType)
        {
            MethodInfo implMethod = null;
            var map = instanceType.GetInterfaceMap(itfType);
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i].Equals(itfMethod))
                {
                    implMethod = map.TargetMethods[i];
                    break;
                }
            }
            return implMethod;
        }
        public static object Invoke(Type itfType, MethodInfo itfMethod, object instance, MethodInfo implMethod, bool eventOp, int timeout, string token, object[] args)
        {
            Type instanceType = instance.GetType();
            var returnType = itfMethod.ReturnType;
            if (eventOp)
            {
                //TODO:事件处理程序的注册和注销
                var d = CreateEventHandler(itfMethod);
            }
            var implAttrs = implMethod.GetCustomAttributes(true);
            var timeoutControl = implAttrs.OfType<TimeoutAttribute>();
            TimeoutAttribute timeoutAttr = null;
            if ((timeoutAttr = timeoutControl.FirstOrDefault()) != null)
            {
                timeout = timeoutAttr.Milliseconds;
            }
            var retVal = itfMethod.Invoke(instance, args);
            var taskVal = retVal as Task;
            if (taskVal != null)
            {
                //service method max timeout is 120s.
                if (timeout > 0)
                {
                    if (timeout > 120 * 1000)
                        timeout = 120 * 1000;
                }
                else
                {
                    timeout = 120 * 1000;
                }
                if (!taskVal.Wait(timeout))
                {
                    throw new TimeoutException("the service call is timeout. a rpc call should finished in 120s or a time overwritting by using TimeoutAttribute.");
                }
                if (returnType.IsGenericType)
                {
                    return returnType.GetProperty("Result").GetValue(taskVal);
                }
                return null;
            }
            else
                return retVal;
        }

        private static Delegate CreateEventHandler(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
            {
                var plist = method.GetParameters();
                var pType = plist[0].ParameterType;
                if (pType.FullName == "System.MulticastDelegate")
                {
                    AssemblyName assemblyName = new AssemblyName(Guid.NewGuid().ToString());

                    AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
                    var moduleBuilder = ab.DefineDynamicModule(assemblyName.Name, string.Concat(assemblyName.Name, ".dll"));
                }
            }
            else
            {

            }
            throw new NotImplementedException();
        }
    }
}
