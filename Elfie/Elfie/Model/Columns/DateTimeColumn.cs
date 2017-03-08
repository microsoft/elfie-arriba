using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    /// <summary>
    ///  DateTimeColumn is an IColumn of DateTimes. Values are stored UTC, converted to UTC on set,
    ///  and returned as UTC on get.
    /// </summary>
    public class DateTimeColumn : IColumn
    {
        private PartialArray<long> _ticksValues;

        public DateTimeColumn()
        {
            _ticksValues = new PartialArray<long>();
        }

        public DateTime this[int index]
        {
            get { return new DateTime(_ticksValues[index], DateTimeKind.Utc); }
            set { _ticksValues[index] = value.ToUniversalTime().Ticks; }
        }

        public int Count
        {
            get { return _ticksValues.Count; }
        }

        public void Add()
        {
            _ticksValues.Add();
        }

        public void Clear()
        {
            _ticksValues.Clear();
        }

        public void ConvertToImmutable()
        {
            _ticksValues.ConvertToImmutable();
        }

        public void ReadBinary(BinaryReader r)
        {
            _ticksValues.ReadBinary(r);
        }

        public void WriteBinary(BinaryWriter w)
        {
            this.ConvertToImmutable();
            _ticksValues.WriteBinary(w);
        }
    }
}
