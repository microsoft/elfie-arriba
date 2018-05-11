// Copyidentity (c) Microsoft. All identitys reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Types;

namespace XForm.Functions
{
    //internal class LastRowValueBuilder : IFunctionBuilder
    //{
    //    public string Name => "LastRowValue";
    //    public string Usage => "LastRowValue({ValueToCopy}, {CopyIfIdentityMatches})";
    //    public Type ReturnType => null;

    //    public IXColumn Build(IXTable source, XDatabaseContext context)
    //    {
    //        IXColumn value = context.Parser.NextColumn(source, context);
    //        IXColumn identity = context.Parser.NextColumn(source, context);

    //        return (IXColumn)Allocator.ConstructGenericOf(typeof(LastRowValue<>), identity.ColumnDetails.Type, value, identity);
    //    }
    //}

    //// NO NO - Separate into two operations, one to get the last row value and one to null if not the same as last row.
    //public class LastRowValue<T, U> : IXColumn
    //{
    //    private IXColumn _value;
    //    private IXColumn _identity;

    //    private U _lastIdentity;
    //    private bool[] _nullBuffer;

    //    private IValueCopier _identityCopier;
    //    private IXArrayComparer<U> _identityComparer;

    //    public ColumnDetails ColumnDetails { get; private set; }
    //    public Type IndicesType => null;

    //    public LastRowValue(IXColumn value, IXColumn identity)
    //    {
    //        _value = value;
    //        _identity = identity;
    //        this.ColumnDetails = new ColumnDetails("LastRowValue", value.ColumnDetails.Type);

    //        ITypeProvider provider = TypeProviderFactory.Get(typeof(U));
    //        _identityCopier = provider.TryGetCopier();
    //        _identityComparer = (IXArrayComparer<U>)provider.TryGetComparer();
    //    }

    //    public Func<object> ComponentGetter(string componentName)
    //    {
    //        return null;
    //    }

    //    public Func<XArray> CurrentGetter()
    //    {
    //        Func<XArray> valueGetter = _value.CurrentGetter();
    //        Func<XArray> identityGetter = _identity.CurrentGetter();

    //        return () => Transform(valueGetter(), identityGetter());
    //    }

    //    public Func<ArraySelector, XArray> SeekGetter()
    //    {
    //        Func<ArraySelector, XArray> valueGetter = _value.SeekGetter();
    //        Func<ArraySelector, XArray> identityGetter = _identity.SeekGetter();
    //        if (valueGetter == null || identityGetter == null) return null;

    //        return (selector) => Transform(valueGetter(selector), identityGetter(selector));
    //    }

    //    public Func<XArray> IndicesCurrentGetter()
    //    {
    //        return null;
    //    }

    //    public Func<ArraySelector, XArray> IndicesSeekGetter()
    //    {
    //        return null;
    //    }

    //    public Func<XArray> ValuesGetter()
    //    {
    //        return null;
    //    }

    //    private XArray Transform(XArray value, XArray identity)
    //    {
    //        // TODO: Compare each identity to the one from the last row.
    //        // When they're the same, return the value from the last row.
    //        // When they're not, return null.
    //        // Need to compare the first identity from each page with the last one from the previous page

    //        float[] valueArray = (float[])value.Array;
    //        U[] identityArray = (U[])identity.Array;

    //        int count = value.Count;
    //        Allocator.AllocateToSize(ref _buffer, count);
    //        Allocator.AllocateToSize(ref _nullBuffer, count);

    //        for (int i = 0; i < count; ++i)
    //        {
    //            int valueIndex = value.Index(i);
    //            int identityIndex = identity.Index(i);

    //            U currentIdentity = identityArray[identityIndex];
    //            if(_identityComparer.WhereEqual(currentIdentity, _lastIdentity))
    //            {

    //            }
    //            else
    //            {
    //                _nullBuffer[i] = true;
    //            }
    //            _buffer[i] = valueArray[valueIndex] * identityArray[identityIndex];
    //            _nullBuffer[i] = (value.NullRows != null && value.NullRows[valueIndex]) || (identity.NullRows != null && identity.NullRows[identityIndex]);
    //        }

    //        return XArray.All(_buffer, value.Count, _nullBuffer);
    //    }
    //}
}
