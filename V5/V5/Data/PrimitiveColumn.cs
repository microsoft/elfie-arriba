using System;
using System.IO;
using V5.Collections;
using V5.Serialization;

namespace V5.Data
{
    public class PrimitiveColumn<T> : IColumn
    {
        public string Name { get; private set; }
        public string TypeIdentifier { get; private set; }

        private CachedLoader _loader;
        private string _identifier;

        private WeakReference<PartialArray<T>> _values;

        public PrimitiveColumn(string name, string typeIdentifier, string parentIdentifier, CachedLoader loader)
        {
            this.Name = name;
            this.TypeIdentifier = typeIdentifier;
            this._loader = loader;
            this._identifier = Path.Combine(parentIdentifier, name, "V." + this.TypeIdentifier + ".bin");
        }

        private PartialArray<T> Values
        {
            get
            {
                PartialArray<T> result;
                if(!_values.TryGetTarget(out result))
                {
                    result = _loader.Get<PartialArray<T>>(this._identifier);
                    _values.SetTarget(result);
                }

                return result;
            }
        }

        public void Save()
        {
            this._loader.Save(this._identifier);
        }

        public int Count => this.Values.Count;

        public void Append(T[] values, int index, int length)
        {
            this.Values.AppendFrom(values, index, length);
        }

        public void SetCapacity(int capacity)
        {
            this.Values.SetCapacity(capacity);
        }

        public void AppendFrom(Array array, int index, int length)
        {
            // TODO: Need conversion at the array level
            this.Values.AppendFrom((T[])array, index, length);
        }

        public void SetValues(Array values, uint[] indices)
        {
            this.Values.SetValues((T[])values, indices);
        }

        public void GetValues(Array result, uint[] indices)
        {
            this.Values.GetValues((T[])result, indices);
        }

        public Array TryGetArray()
        {
            return this.Values.TryGetArray();
        }
    }
}
