// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm.Types
{
    public interface IColumnReader : IDisposable
    {
        /// <summary>
        ///  Row Count in read file.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///  Read the rows for the given selector into an XArray.
        /// </summary>
        /// <param name="selector">ArraySelector for desired rows.</param>
        /// <returns>XArray with values for selected rows.</returns>
        XArray Read(ArraySelector selector);
    }

    public interface IColumnWriter : IDisposable
    {
        /// <summary>
        ///  Type of the values being written.
        /// </summary>
        Type WritingAsType { get; }

        /// <summary>
        ///  Check whether the new values can be written within the file size limit for the column.
        /// </summary>
        /// <param name="xarray">XArray of values to write</param>
        /// <returns>True if the new values fit in the file size limit for the column type, False otherwise.</returns>
        bool CanAppend(XArray xarray);

        /// <summary>
        ///  Append the provided values to the file.
        /// </summary>
        /// <param name="xarray">XArray with current set of values</param>
        void Append(XArray xarray);
    }

    public interface ITypeProvider
    {
        string Name { get; }
        Type Type { get; }

        IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, CachingOption option);
        IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath);

        NegatedTryConvert TryGetNegatedTryConvert(Type sourceType, Type targetType, object defaultValue);

        IXArrayComparer TryGetComparer();

        IValueCopier TryGetCopier();
    }

    public static class TypeProviderExtensions
    {
        public static ComparerExtensions.Comparer TryGetComparer(this ITypeProvider typeProvider, CompareOperator op)
        {
            IXArrayComparer comparer = typeProvider.TryGetComparer();
            if (comparer == null) return null;
            return comparer.TryBuild(op);
        }

        public static Func<XArray, XArray> TryGetConverter(Type sourceType, Type targetType, ValueKinds errorOnKinds, object defaultValue, ValueKinds changeToDefaultKinds)
        {
            return TypeConverterFactory.TryGetConverter(sourceType, targetType, errorOnKinds, defaultValue, changeToDefaultKinds);
        }
    }
}
