using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XForm
{
    public class ColumnDetails
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public bool Nullable { get; private set; }
    }

    public struct ColumnSpan
    {
        public Array Array { get; private set; }

        public int Index { get; private set; }
        public int Length { get; private set; }


    }

    public interface ITabularSource
    {
        IReadOnlyCollection<ColumnDetails> Columns { get; }

    }


    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
