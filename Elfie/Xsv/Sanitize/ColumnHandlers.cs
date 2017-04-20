using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Collections.Generic;

namespace Xsv.Sanitize
{
    public interface IColumnHandler
    {
        void Sanitize(ITabularValue value, ITabularWriter writer);
    }

    public class EchoColumnHandler : IColumnHandler
    {
        private HashSet<String8> EchoValues { get; set; }
        private IColumnHandler Inner { get; set; }

        public EchoColumnHandler(HashSet<String8> echoValues, IColumnHandler inner)
        {
            this.EchoValues = echoValues;
            this.Inner = inner;
        }

        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            String8 value8 = value.ToString8();
            if (this.EchoValues.Contains(value8))
            {
                writer.Write(value8);
            }
            else
            {
                this.Inner.Sanitize(value, writer);
            }
        }
    }

    public class DropColumnHandler : IColumnHandler
    {
        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            // Don't output the column at all
        }
    }

    public class KeepColumnHandler : IColumnHandler
    {
        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            // Write the value as-is
            writer.Write(value.ToString8());
        }
    }

    public class MapColumnHandler : IColumnHandler
    {
        private int HashKeyHash { get; set; }
        private ISanitizeMapper Mapper { get; set; }
        private SanitizeContext Context { get; set; }
        private byte[] ConvertBuffer { get; set; }

        public MapColumnHandler(int hashKeyHash, string mapperName, ISanitizerProvider provider, string[] args)
        {
            this.HashKeyHash = hashKeyHash;
            this.ConvertBuffer = new byte[30];
            this.Context = new SanitizeContext() { Provider = provider, Args = args };
            this.Mapper = provider.Mapper(mapperName);
        }

        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            this.Context.Hash = Hash(value.ToString8(), this.HashKeyHash);
            writer.Write(String8.Convert(this.Mapper.Generate(this.Context), this.ConvertBuffer));
        }

        public static uint Hash(object value, int hashKeyHash)
        {
            return (uint)((value.GetHashCode() ^ hashKeyHash).GetHashCode());
        }
    }
}
