using System;
using System.Reflection;

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
        public static Func<ulong[], int> GetBitVectorCount()
        {
            // TODO: Need to make this model generic
            Assembly xformNative = Assembly.Load("XForm.Native");
            Type xformNativeType = xformNative.GetType("XForm.Native.BitVectorN");
            MethodInfo method = xformNativeType.GetMethod("Count", new Type[] { typeof(ulong[]) });
            return (Func<ulong[], int>)Delegate.CreateDelegate(typeof(Func<ulong[], int>), method);
        }
    }
}
