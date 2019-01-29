using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp.Internal
{
    class DelegateHelper
    {
        static Dictionary<Guid, Delegate> delegates = new Dictionary<Guid, Delegate>();
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Delegate InstanceCombine(Delegate source, Delegate value)
        {
            if (RpcHead.Current == null)//client
            {
                return Delegate.Combine(source, value);
            }
            Guid instanceId = RpcHead.Current.InstanceId;
            Delegate fixedSource = null;
            if (!delegates.ContainsKey(instanceId))
            {
                if (source != null)
                {
                    delegates.Add(instanceId, source);
                    fixedSource = source;
                }
            }
            else
            {
                fixedSource = delegates[instanceId];
            }
            var retVal = Delegate.Combine(fixedSource, value);
            if (retVal == null)
            {
                delegates.Remove(instanceId);
            }
            else
            {
                delegates[instanceId] = retVal;
            }
            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Delegate InstanceRemove(Delegate source, Delegate value)
        {
            if (RpcHead.Current == null)//client
            {
                return Delegate.Remove(source, value);
            }
            Guid instanceId = RpcHead.Current.InstanceId;
            Delegate fixedSource = null;
            if (!delegates.ContainsKey(instanceId))
            {
                if (source != null)
                {
                    delegates.Add(instanceId, source);
                    fixedSource = source;
                }
            }
            else
            {
                fixedSource = delegates[instanceId];
            }
            var retVal = Delegate.Remove(fixedSource, value);
            if (retVal == null)
            {
                delegates.Remove(instanceId);
            }
            else
            {
                delegates[instanceId] = retVal;
            }
            return retVal;
        }

        //public static MethodInfo EqualInvocationLists = typeof(Delegate).GetMethod("EqualInvocationLists", BindingFlags.Instance | BindingFlags.NonPublic);
        //public static MethodInfo DeleteFromInvocationList = typeof(Delegate).GetMethod("DeleteFromInvocationList", BindingFlags.Instance | BindingFlags.NonPublic);
        //public static MethodInfo TrySetSlot = typeof(Delegate).GetMethod("TrySetSlot", BindingFlags.Instance | BindingFlags.NonPublic);
        //public static MethodInfo NewMulticastDelegate = typeof(Delegate).GetMethod("NewMulticastDelegate", BindingFlags.Instance | BindingFlags.NonPublic);
        //public static FieldInfo _invocationCount = typeof(MulticastDelegate).GetField("_invocationCount", BindingFlags.Instance | BindingFlags.NonPublic);
        //public static FieldInfo _invocationList = typeof(MulticastDelegate).GetField("_invocationList", BindingFlags.Instance | BindingFlags.NonPublic);
        //public static MethodInfo InternalEqualTypes = typeof(Delegate).GetMethod("InternalEqualTypes", BindingFlags.Static | BindingFlags.NonPublic);


        //public static Delegate Combine(Delegate source, Delegate follow)
        //{
        //    if (source == null)
        //    {
        //        return follow;
        //    }
        //    if (follow == null)
        //    {
        //        return source;
        //    }
        //    if (!(bool)InternalEqualTypes.Invoke(null, new object[] { source, follow }))
        //    {
        //        throw new ArgumentException("Delegates must be of the same type.");
        //    }
        //    MulticastDelegate multicastDelegate = (MulticastDelegate)follow;
        //    int num = 1;
        //    object[] array = _invocationList.GetValue(multicastDelegate) as object[];
        //    if (array != null)
        //    {
        //        num = (int)_invocationCount.GetValue(multicastDelegate);
        //    }
        //    object[] array2 = _invocationList.GetValue(source) as object[];
        //    int num2;
        //    object[] array3;
        //    if (array2 == null)
        //    {
        //        num2 = 1 + num;
        //        array3 = new object[num2];
        //        array3[0] = source;
        //        if (array == null)
        //        {
        //            array3[1] = multicastDelegate;
        //        }
        //        else
        //        {
        //            for (int i = 0; i < num; i++)
        //            {
        //                array3[1 + i] = array[i];
        //            }
        //        }
        //        return NewMulticastDelegate.Invoke(source, new object[] { array3, num2 }) as Delegate;
        //    }
        //    int num3 = (int)_invocationCount.GetValue(source);
        //    num2 = num3 + num;
        //    array3 = null;
        //    if (num2 <= array2.Length)
        //    {
        //        array3 = array2;
        //        if (array == null)
        //        {
        //            if (!(bool)TrySetSlot.Invoke(source, new object[] { array3, num3, multicastDelegate }))
        //            {
        //                array3 = null;
        //            }
        //        }
        //        else
        //        {
        //            for (int j = 0; j < num; j++)
        //            {
        //                if (!(bool)TrySetSlot.Invoke(source, new object[] { array3, num3 + j, array[j] }))
        //                {
        //                    array3 = null;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    if (array3 == null)
        //    {
        //        int k;
        //        for (k = array2.Length; k < num2; k *= 2)
        //        {
        //        }
        //        array3 = new object[k];
        //        for (int l = 0; l < num3; l++)
        //        {
        //            array3[l] = array2[l];
        //        }
        //        if (array == null)
        //        {
        //            array3[num3] = multicastDelegate;
        //        }
        //        else
        //        {
        //            for (int m = 0; m < num; m++)
        //            {
        //                array3[num3 + m] = array[m];
        //            }
        //        }
        //    }
        //    return NewMulticastDelegate.Invoke(source, new object[] { array3, num2, true }) as MulticastDelegate;
        //}
        //public static Delegate Remove(Delegate source, Delegate value)
        //{
        //    if (source == null)
        //    {
        //        return null;
        //    }
        //    if (value == null)
        //    {
        //        return source;
        //    }
        //    if (!(bool)InternalEqualTypes.Invoke(null, new object[] { source, value }))
        //    {
        //        throw new ArgumentException("Delegates must be of the same type.");
        //    }
        //    MulticastDelegate multicastDelegate = value as MulticastDelegate;
        //    if (multicastDelegate == null)
        //    {
        //        return source;
        //    }
        //    if (!(_invocationList.GetValue(multicastDelegate) is object[]))
        //    {
        //        object[] array = _invocationList.GetValue(source) as object[];
        //        if (array == null)
        //        {
        //            if (source.Equals(value))
        //            {
        //                return null;
        //            }
        //        }
        //        else
        //        {
        //            int num = (int)_invocationCount.GetValue(source);
        //            int num2 = num;
        //            while (--num2 >= 0)
        //            {
        //                if (value.Equals(array[num2]))
        //                {
        //                    if (num == 2)
        //                    {
        //                        return (Delegate)array[1 - num2];
        //                    }
        //                    object[] invocationList = DeleteFromInvocationList.Invoke(source, new object[] { array, num, num2, 1 }) as object[];
        //                    return NewMulticastDelegate.Invoke(source, new object[] { invocationList, num - 1, true }) as Delegate;
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        object[] array2 = _invocationList.GetValue(source) as object[];
        //        if (array2 != null)
        //        {
        //            int num3 = (int)_invocationCount.GetValue(source);
        //            int num4 = (int)_invocationCount.GetValue(multicastDelegate);
        //            int i = num3 - num4;
        //            while (i >= 0)
        //            {
        //                if ((bool)EqualInvocationLists.Invoke(source, new object[] { array2, _invocationCount.GetValue(multicastDelegate) as object[], i, num4 }))
        //                {
        //                    if (num3 - num4 == 0)
        //                    {
        //                        return null;
        //                    }
        //                    if (num3 - num4 == 1)
        //                    {
        //                        return (Delegate)array2[(i != 0) ? 0 : (num3 - 1)];
        //                    }
        //                    object[] invocationList2 = DeleteFromInvocationList.Invoke(source, new object[] { array2, num3, i, num4 }) as object[];
        //                    return NewMulticastDelegate.Invoke(source, new object[] { invocationList2, num3 - num4, true }) as MulticastDelegate;
        //                }
        //                else
        //                {
        //                    i--;
        //                }
        //            }
        //        }
        //    }
        //    return source;
        //}
    }
}
