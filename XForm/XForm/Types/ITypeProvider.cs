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
        int Count { get; }
        XArray Read(ArraySelector selector);
    }

    public interface IColumnWriter : IDisposable
    {
        Type WritingAsType { get; }
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
