using System.Text;
namespace Arriba.TfsWorkItemCrawler.ItemProviders
{
    using Arriba.Extensions;
    using Arriba.Serialization;
    using System;

    public static class ItemProviderUtilities
    {
        // Field length limit is 10MB
        public const int FieldLengthLimitBytes = 10 * 1024 * 1024;

        public static IItemProvider Build(CrawlerConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config", "config is null.");
            }

            if (String.Equals(config.ItemProvider, "TfsItemProvider", StringComparison.InvariantCultureIgnoreCase))
            {
                return new TfsItemProvider(config);
            }
            else
            {
                throw new InvalidOperationException(String.Format("{0} is an unknown Item Provider", config.ItemProvider));
            }
        }

        public const string CutoffLocationFormatString = @"Tables\{0}\Cutoff.{1}.txt";
        public static DateTime LoadLastCutoff(string tableName, string configurationName, bool rebuild)
        {
            if (String.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("tableName is null or empty.", "tableName");
            }

            if (String.IsNullOrEmpty(configurationName))
            {
                throw new ArgumentException("configurationName is null or empty.", "configurationName");
            }

            DateTime allItemsCutoff = DateTime.UtcNow.AddYears(-20);
            DateTime cutoff;

            if (rebuild)
            {
                cutoff = allItemsCutoff;
            }
            else
            {
                cutoff = TextSerializer.ReadDateTime(String.Format(CutoffLocationFormatString, tableName, configurationName), allItemsCutoff);
            }

            return cutoff;
        }

        public static void SaveLastCutoff(string tableName, string configurationName, DateTime cutoff)
        {
            // Write the new cutoff as long as it's not still the default one
            if (cutoff.Year > (DateTime.UtcNow.Year - 19))
            {
                TextSerializer.Write(cutoff, String.Format(CutoffLocationFormatString, tableName, configurationName));
            }
        }

        public static object Canonicalize(object value)
        {
            if (value is DateTime) return CanonicalizeDateTime((DateTime)value);

            if (value is string)
            {
                string valueString = (string)value;
                if(valueString.Length > FieldLengthLimitBytes)
                {
                    valueString = valueString.Substring(0, FieldLengthLimitBytes);
                }

                return valueString.CanonicalizeNewlines();
            }

            return value;
        }

        public static DateTime CanonicalizeDateTime(DateTime value)
        {
            // Convert all DateTimes to UTC
            DateTime result = value.ToUniversalTime();

            // Truncate to previous whole second
            return new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, DateTimeKind.Utc);
        }

        public static string ConvertLineBreaksToHtml(string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            StringBuilder result = new StringBuilder();

            char last = '\0';
            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];

                if (c == '\n')
                {
                    // \n without \r? Replace with <br />
                    if (last != '\r')
                    {
                        result.Append("<br />");
                    }
                }
                else if (c == '\r')
                {
                    //replace all carriage return with <br />
                    result.Append("<br />");
                }
                else
                {
                    //output everything except \r or \n
                    result.Append(c);
                }

                last = c;
            }

            return result.ToString();
        }
    }
}
