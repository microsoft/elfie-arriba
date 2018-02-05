using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Functions
{
    public class Coalesce : IFunctionBuilder
    {
        public Type ReturnType => throw new NotImplementedException();

        public string Usage => "Coalesce({Col|Func|Const}, ...)";

        public string Name => "Coalesce";

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            List<IXColumn> columns = new List<IXColumn>();

            do
            {
                IXColumn column = context.Parser.NextColumn(source, context);
                columns.Add(column);
                if (System.String.IsNullOrEmpty(column.ColumnDetails.Name)) throw new ArgumentException($"Column {columns.Count} passed to 'Column' wasn't assigned a name. Use 'AS [Name]' to assign names to every column selected.");
            } while (context.Parser.HasAnotherPart);

            Array array = new String8Block[columns[0]];
            // Start with first column
            // If non nulls, return first column
            // Else, take all non-nulls from first, and replace nulls with non-nulls from second.
            throw new NotImplementedException();
        }
    }
}
