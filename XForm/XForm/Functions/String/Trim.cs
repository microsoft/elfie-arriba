using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using XForm.Data;

namespace XForm.Functions.String
{
    internal class TrimBuilder : IFunctionBuilder
    {
        public string Name => "Trim";
        public string Usage => "Trim([String8])";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new SimpleTransformFunction<String8, String8>(
                context.Parser.NextColumn(source, context), 
                (string8) => string8.Trim());
        }
    }
}
