using Microsoft.CodeAnalysis.Elfie.Indexer;
using System;
using System.Diagnostics;

namespace SymbolForge
{
    /// <summary>
    ///  SymbolForge changes a PDB so that Visual Studio considers it a match for a given DLL.
    ///  
    ///  This will only be useful if the PDB was built from the same sources with the same compiler.
    ///  DLLs have a GUID and 'Age' burned into them which has to match in the PDB.
    ///  This code reads the GUID and Age and writes those values into the desired PDB.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: SymbolForge <DllPath> <PdbPath>");
                Console.WriteLine("SymbolForge writes the GUID and Age from a DLL into a PDB so that VS considers them matching.");
                return -2;
            }

            try
            {
                string dllPath = args[0];
                string pdbPath = args[1];

                RsDsSignature signature = Assembly.ReadRsDsSignature(dllPath);
                Console.WriteLine($"{dllPath} signature: {signature}");

                Assembly.WriteRsDsSignature(pdbPath, signature);
                Console.WriteLine($"Done. {pdbPath} signature set to {signature}.");
                return 0;
            }
            catch(Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"ERROR: {ex}");
                return -1;
            }
        }
    }
}
