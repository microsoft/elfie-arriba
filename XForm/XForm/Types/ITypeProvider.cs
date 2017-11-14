using System;
using XForm.Data;

namespace XForm.Types
{
    public interface IColumnReader : IDisposable
    {
        int Count { get; }
        DataBatch Read(ArraySelector selector);
    }

    public interface IColumnWriter : IDisposable
    {
        void Append(DataBatch batch);
    }

    public interface ITypeProvider
    {
        string Name { get; }
        Type Type { get; }

        IColumnReader BinaryReader(string columnPath);
        IColumnWriter BinaryWriter(string columnPath);

        Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue);
    }
}
