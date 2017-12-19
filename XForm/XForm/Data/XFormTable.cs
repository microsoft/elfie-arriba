using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.IO;

namespace XForm.Data
{
    /// <summary>
    ///  XFormTable provides static helper functions to expose XForm in a friendly way
    ///  in code.
    /// </summary>
    public static class XFormTable
    {
        public static ArrayTable FromArrays(int totalCount)
        {
            return new ArrayTable(totalCount);
        }
    }
}
