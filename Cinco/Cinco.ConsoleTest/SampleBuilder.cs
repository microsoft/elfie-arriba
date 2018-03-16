using System;
using System.IO;

namespace Cinco.ConsoleTest
{
    public class SampleBuilder
    {
        static string CodeSeparator = "// ========================================";

        public static void ConcatenateCode(string directoryPath, StreamWriter writer)
        {
            foreach (string codeFilePath in Directory.GetFiles(directoryPath, "*.cs"))
            {
                writer.Write("// ");
                writer.WriteLine(codeFilePath);
                writer.WriteLine(CodeSeparator);
                writer.Flush();

                using (var reader = File.OpenRead(codeFilePath))
                {
                    reader.CopyTo(writer.BaseStream);
                }

                writer.WriteLine(CodeSeparator);
                writer.WriteLine();
            }

            foreach (string subdirectory in Directory.GetDirectories(directoryPath))
            {
                ConcatenateCode(subdirectory, writer);
            }
        }

        public static void ConcatenateCode(string directoryPath, string outputFilePath)
        {
            if (!File.Exists(outputFilePath))
            {
                Console.WriteLine($"Concatenating C# under '{directoryPath}' into '{outputFilePath}'...");

                using (StreamWriter writer = new StreamWriter(outputFilePath))
                {
                    ConcatenateCode(directoryPath, writer);
                    writer.Flush();

                    Console.WriteLine($"Done. {writer.BaseStream.Position.ToSizeString()} written.");
                }
            }
        }
    }
}
