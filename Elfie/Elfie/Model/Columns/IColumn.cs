// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    /// <summary>
    ///  IColumn wraps a column of values of a particular type in an Elfie data structure.
    ///  Columns may have mutable (changeable) and immutable (read-only) forms.
    ///  Columns use IBinarySerializable to implement fast serialization.
    ///  Use 'Add' to resize the column and then set the value with an indexer.
    ///  
    ///  IColumn *does not* have an indexer returning object because that causes boxing for value types.
    ///  IColumn&lt;T&gt; is not defined because using an interface prevents inlining the indexer, which is a
    ///  significant performance impact for single array reads/writes.
    ///  
    ///  Usage:
    ///   IColumn column = new ...
    ///   
    ///   for(int i = 0; i &lt; source.Length; ++i)
    ///   {
    ///       column.Add()
    ///       column[i] = source[i];
    ///   }
    ///   
    ///   column.ConvertToImmutable();
    ///   column.WriteBinary(stream);
    ///   
    ///   ...
    ///   
    ///   IColumn columnForSearch = new ...
    ///   column.ReadBinary(stream);
    ///   
    ///   T value = column[i];
    /// </summary>
    public interface IColumn : IBinarySerializable
    {
        /// <summary>
        ///  Get the count of items in this column
        /// </summary>
        int Count { get; }

        /// <summary>
        ///  Reset this column to be empty. May reuse previously allocated memory.
        /// </summary>
        void Clear();

        /// <summary>
        ///  Add room for one more item (with a default value).
        /// </summary>
        void Add();

        /// <summary>
        ///  Set the column to have the specific number of elements with default values.
        /// </summary>
        void SetCount(int count);

        /// <summary>
        ///  Convert this column to immutable (read only) form, if applicable.
        ///  Add calls and sets may throw exceptions after ConvertToImmutable.
        /// </summary>
        void ConvertToImmutable();
    }
}
