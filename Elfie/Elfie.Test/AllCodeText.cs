using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Test
{
    /// <summary>
    ///  Test Helper which builds a String8 of all Elfie code for tests
    ///  which require a lot of text to scan.
    /// </summary>
    public static class AllCodeText
    {
        private static String8 _allCode8;

        public static String8 AllCode8
        {
            get
            {
                if (_allCode8.IsEmpty()) LoadFullCode();
                return _allCode8;
            }
        }

        private static void LoadFullCode()
        {
            long totalCodeLength = 0;

            FileInfo[] codeFiles = new DirectoryInfo(@"..\..\..").GetFiles("*.cs", SearchOption.AllDirectories);
            foreach (FileInfo file in codeFiles)
            {
                totalCodeLength += file.Length;
            }

            int lengthRead = 0;
            byte[] allCode = new byte[totalCodeLength];
            foreach (FileInfo file in codeFiles)
            {
                using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    lengthRead += stream.Read(allCode, lengthRead, (int)file.Length);
                }
            }

            _allCode8 = new String8(allCode, 0, allCode.Length);
        }
    }
}
