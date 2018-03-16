using System.Runtime.InteropServices;
using System.Security;

namespace Cinco.Extensions
{
    public static class String
    {
        internal class NativeMethods
        {
            [DllImport("Cinco.Native.dll", PreserveSig = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            public unsafe static extern int IndexOf(ushort* text, int textLength, ushort* value, int valueLength);
        }

        public unsafe static int IndexOfN(this string text, string value, int fromIndex = 0)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value)) return -1;

            fixed(char* textPtr = text)
            fixed(char* valuePtr = value)
            {
                int innerIndex = NativeMethods.IndexOf((ushort*)(textPtr + fromIndex), text.Length - fromIndex, (ushort*)valuePtr, value.Length);
                return (innerIndex == -1 ? innerIndex : fromIndex + innerIndex);
            }
        }
    }
}
