using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Xsv.Sanitize
{
    public static class Resource
    {
        public static string[] ReadAllStreamLines(string streamName)
        {
            Assembly xsv = Assembly.GetExecutingAssembly();
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(xsv.GetManifestResourceStream(streamName)))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            return lines.ToArray();
        }
    }
}
