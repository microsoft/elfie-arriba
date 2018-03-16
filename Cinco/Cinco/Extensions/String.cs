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

        public unsafe static int IndexOfN(this string text, string value)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value)) return -1;

            fixed(char* textPtr = text)
            fixed(char* valuePtr = value)
            {
                return NativeMethods.IndexOf((ushort*)textPtr, text.Length, (ushort*)valuePtr, value.Length);
            }
        }
    }
}
