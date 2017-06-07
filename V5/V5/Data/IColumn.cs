using System;

namespace V5.Data
{
    public interface IColumn
    {
        // Column Basics
        string Name { get; }
        string TypeIdentifier { get; }
        int Count { get; }

        // Put values in
        void SetCapacity(int capacity);
        void AppendFrom(Array array, int index, int length);
        void SetValues(Array values, uint[] indices);

        // Get specific values out
        void GetValues(Array result, uint[] indices);

        // Get everything out (if fully loaded and has an internal array)
        Array TryGetArray();
    }

    // Do I need a typed column at all? Without one, how do aggregations work?
}
