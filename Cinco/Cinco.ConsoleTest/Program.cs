using Cinco.Extensions;
using System;

namespace Cinco.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string text = "Interesting text";
            string value = "est";

            int index = text.IndexOfN(value);
            Console.WriteLine($"{index}");
        }
    }
}
