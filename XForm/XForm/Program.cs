using Microsoft.CodeAnalysis.Elfie.Serialization;
using XForm.Sources;
using XForm.Writers;

namespace XForm
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get this to work. What's up with Elfie BadImageFormatException?
            // Copy from TSV to CSV
            using (TabularFileWriter writer = new TabularFileWriter(new TabularFileReader(TabularFactory.BuildReader(args[0])), TabularFactory.BuildWriter(args[1])))
            {
                writer.Copy();
            }
        }
    }
}
