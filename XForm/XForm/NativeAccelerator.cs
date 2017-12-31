using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XForm.Data;
using XForm.Types;
using XForm.Types.Comparers;

namespace XForm
{
    /// <summary>
    ///  NativeAccelerator allows enabling or disabling accelerated C++ implementations
    ///  of key XForm operations.
    /// </summary>
    /// <remarks>
    ///   See https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
    /// </remarks>
    public static class NativeAccelerator
    {
        public static T GetMethod<T>(string namespaceAndTypeName, string methodName)
        {
            Type delegateOrFuncType = typeof(T);
            List<Type> arguments = new List<Type>(delegateOrFuncType.GetMethod("Invoke").GetParameters().Select((pi) => pi.ParameterType));

            Assembly xformNative = Assembly.Load("XForm.Native");
            if (xformNative == null) throw new ArgumentException("XForm.Native.dll wasn't found.");

            Type xformNativeType = xformNative.GetType(namespaceAndTypeName);
            if (xformNativeType == null) throw new ArgumentException($"Type {namespaceAndTypeName} not found in XForm.Native.dll.");

            MethodInfo method = xformNativeType.GetMethod(methodName, arguments.ToArray());
            if (method == null) throw new ArgumentException($"Static Method {methodName} not found on {namespaceAndTypeName} in XForm.Native.dll with expected arguments.");

            return (T)(object)Delegate.CreateDelegate(delegateOrFuncType, method);
        }

        public static void Enable()
        {
            BitVector.s_nativeCount = GetMethod<Func<ulong[], int>>("XForm.Native.BitVectorN", "Count");
            BitVector.s_nativePage = GetMethod<BitVector.PageSignature>("XForm.Native.BitVectorN", "Page");

            UshortComparer.s_WhereNative = GetMethod<ComparerExtensions.Where<ushort>>("XForm.Native.Comparer", "Where");
            ShortComparer.s_WhereNative = GetMethod<ComparerExtensions.Where<short>>("XForm.Native.Comparer", "Where");
            //ByteComparer.s_WhereNative = GetMethod<ComparerExtensions.Where<byte>>("XForm.Native.Comparer", "Where");
            //SbyteComparer.s_WhereNative = GetMethod<ComparerExtensions.Where<sbyte>>("XForm.Native.Comparer", "Where");

            UshortComparer.s_WhereSingleNative = GetMethod<ComparerExtensions.WhereSingle<ushort>>("XForm.Native.Comparer", "Where");
            ShortComparer.s_WhereSingleNative = GetMethod<ComparerExtensions.WhereSingle<short>>("XForm.Native.Comparer", "Where");
            ByteComparer.s_WhereSingleNative = GetMethod<ComparerExtensions.WhereSingle<byte>>("XForm.Native.Comparer", "Where");
            SbyteComparer.s_WhereSingleNative = GetMethod<ComparerExtensions.WhereSingle<sbyte>>("XForm.Native.Comparer", "Where");
        }
    }
}
