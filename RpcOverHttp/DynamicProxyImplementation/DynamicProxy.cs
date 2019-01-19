using System;
using System.Linq;
using System.Text;
using System.Reflection;

namespace DynamicProxyImplementation
{
    public abstract class DynamicProxy : IDisposable
    {
        private static object dummyOut;
        public static MethodInfo TryGetMemberMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TryGetMember(null, null, out dummyOut));
        public static MethodInfo TrySetMemberMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TrySetMemberInternal(null, null, null));
        public static MethodInfo TrySetEventMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TrySetEvent(null, null, null, true));
        public static MethodInfo TryInvokeMemberMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TryInvokeMember(null, 0, false, null, out dummyOut));

        public static MethodInfo TryInvokeEventHandlerMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TryInvokeEventHandler(null, null, null, null, out dummyOut));

        protected DynamicProxy()
        {
        }

        protected abstract bool TryInvokeMember(Type interfaceType, int id, bool eventOp, object[] args, out object result);

        protected abstract bool TryInvokeEventHandler(Type interfaceType, Type handlerType, string name, object[] args, out object resul);

        protected abstract bool TrySetMember(Type interfaceType, string name, object value);

        protected abstract bool TryGetMember(Type interfaceType, string name, out object result);

        protected abstract bool TrySetEvent(Type interfaceType, string name, object value, bool add);
        protected bool TrySetEventInternal(Type interfaceType, string name, object value, bool add)
        {
            return TrySetEvent(interfaceType, name, value, add);
        }

        protected bool TrySetMemberInternal(Type interfaceType, string name, object value)
        {
            return TrySetMember(interfaceType, name, value);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DynamicProxy()
        {
            Dispose(false);
        }
    }
}
