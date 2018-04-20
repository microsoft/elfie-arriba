using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace XForm.CST
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 2)
                {
                    Console.WriteLine("Usage: XForm.CST [SarifFolderPath] [XFormDatabaseRootPath]");
                    return -2;
                }

                string sarifInputFolderPath = args[0];
                string xformDatabaseRoot = args[1];
                string csvOutputFilePath = Path.Combine(xformDatabaseRoot, $@"Source\Sarif\Full\{DateTime.UtcNow:yyyy.MM.dd HH.mm.ssZ}\Data.csv");

                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    ContractResolver = SarifContractResolver.Instance,
                    Formatting = Formatting.Indented
                };

                using (new TraceWatch($"Converting '{sarifInputFolderPath}\\*.sarif' to '{csvOutputFilePath}'..."))
                {
                    // Create output directory
                    Directory.CreateDirectory(Path.GetDirectoryName(csvOutputFilePath));

                    using (var writer = TabularFactory.BuildWriter(csvOutputFilePath))
                    {
                        var visitor = new SarifMessageStream(writer);

                        foreach (string filePath in Directory.GetFiles(sarifInputFolderPath, "*.sarif"))
                        {
                            Trace.WriteLine($"Loading '{filePath}'...");
                            string actualSarif = File.ReadAllText(filePath);
                            SarifLog actualLog = JsonConvert.DeserializeObject<SarifLog>(actualSarif, settings);

                            Trace.WriteLine($"Appending to '{csvOutputFilePath}'...");
                            visitor.Visit(actualLog);
                        }
                    }
                }

                Trace.WriteLine("Done.");
                return 0;
            }
            catch(Exception ex) when (!Debugger.IsAttached)
            {
                Trace.WriteLine($"ERROR: {ex}");
                return -1;
            }
        }
    }

    public class SarifProperty
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public SarifProperty(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }
    }

    public class SarifMessageStream : SarifRewritingVisitor
    {
        private bool HasSetColumns { get; set; }
        private List<SarifProperty> ResultProperties { get; set; }

        private Dictionary<string, string> FileUrlToDeepLink { get; set; }

        public SarifMessageStream(ITabularWriter tabularWriter)
        {
            _block = new String8Block();
            _writer = tabularWriter;

            ResultProperties = new List<SarifProperty>();
            FileUrlToDeepLink = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private String8Block _block;
        private ITabularWriter _writer;

        public override Run VisitRun(Run node)
        {
            FileUrlToDeepLink.Clear();

            foreach(var file in node.Files)
            {
                string linkUri;
                if (file.Value.TryGetProperty("DeepLink", out linkUri))
                {
                    FileUrlToDeepLink[file.Key] = linkUri;
                }
            }

            return base.VisitRun(node);
        }

        public override Result VisitResult(Result result)
        {
            _block.Clear();

            // Identify columns on first row
            if (HasSetColumns == false) IdentifyColumns(result);

            // Write a row for each location of the result
            foreach (Location loc in result.Locations)
            {
                _writer.Write(_block.GetCopy(result.RuleId));
                _writer.Write(_block.GetCopy(result.Message));
                _writer.Write(_block.GetCopy(result.ToolFingerprintContribution));

                PhysicalLocation pLoc = loc.ResultFile ?? loc.AnalysisTarget;
                if (pLoc == null)
                {
                    _writer.Write(String8.Empty);
                    _writer.Write(String8.Empty);
                    _writer.Write(String8.Empty);
                    _writer.Write(String8.Empty);
                }
                else
                {
                    string uriString = loc.ResultFile.Uri.ToString();
                    string uriLink;
                    FileUrlToDeepLink.TryGetValue(uriString, out uriLink);
                    _writer.Write(_block.GetCopy(uriLink));
                    _writer.Write(_block.GetCopy(uriString));
                    _writer.Write(loc.ResultFile.Region.StartLine);
                    _writer.Write(loc.ResultFile.Region.StartColumn);
                }

                foreach(SarifProperty prop in ResultProperties)
                {
                    _writer.Write(_block.GetCopy(GetPropertyString(result, prop)));
                }

                _writer.NextRow();
            }

            return base.VisitResult(result);
        }

        private static string GetPropertyString(Result result, SarifProperty prop)
        {
            if (prop.Type == typeof(string))
            {
                string value;
                if (result.TryGetProperty(prop.Name, out value)) return value;
            }
            else if (prop.Type == typeof(int))
            {
                int valueInt;
                if (result.TryGetProperty<int>(prop.Name, out valueInt)) return valueInt.ToString();
            }
            else if (prop.Type == typeof(float))
            {
                float valueFloat;
                if (result.TryGetProperty<float>(prop.Name, out valueFloat)) return valueFloat.ToString();
            }
            else if (prop.Type == typeof(bool))
            {
                bool valueBool;
                if (result.TryGetProperty<bool>(prop.Name, out valueBool)) return valueBool.ToString();
            }
            else if (prop.Type == typeof(DateTime))
            {
                DateTime valueDateTime;
                if (result.TryGetProperty<DateTime>(prop.Name, out valueDateTime)) return valueDateTime.ToString("u");
            }

            return null;
        }

        private void IdentifyColumns(Result result)
        {
            List<string> columns = new List<string>();

            // Always
            columns.Add("RuleId");
            columns.Add("Message");
            columns.Add("ToolFingerprintContribution");

            // If location is found
            if (result.Locations.Count > 0)
            {
                Location first = result.Locations[0];
                columns.Add("Location.DeepLink");
                columns.Add("Location.Uri");
                columns.Add("Location.StartLine");
                columns.Add("Location.StartColumn");
            }

            // Read dynamically
            foreach (string name in result.PropertyNames)
            {
                Type type = IdentifyType(result, name);
                if (type != null)
                {
                    ResultProperties.Add(new SarifProperty(name, type));
                    columns.Add($"Properties.{name}");
                }
            }

            // Set the writer columns (only for the first Sarif file)
            _writer.SetColumns(columns);

            HasSetColumns = true;
        }

        // Horrible Hack. No way to tell the type from the Sarif SDK
        private static Type IdentifyType(Result result, string propertyName)
        {
            try
            {
                string valueString;
                if (result.TryGetProperty(propertyName, out valueString)) return typeof(String);
            }
            catch (InvalidOperationException)
            { }
            catch (JsonReaderException)
            { }

            try
            {
                int valueInt;
                if (result.TryGetProperty<int>(propertyName, out valueInt)) return typeof(int);
            }
            catch (InvalidOperationException)
            { }
            catch (JsonReaderException)
            { }

            try
            {
                float valueFloat;
                if (result.TryGetProperty<float>(propertyName, out valueFloat)) return typeof(float);
            }
            catch (InvalidOperationException)
            { }
            catch (JsonReaderException)
            { }

            try
            {
                bool valueBool;
                if (result.TryGetProperty<bool>(propertyName, out valueBool)) return typeof(bool);
            }
            catch (InvalidOperationException)
            { }
            catch (JsonReaderException)
            { }

            try
            {
                DateTime valueDateTime;
                if (result.TryGetProperty<DateTime>(propertyName, out valueDateTime)) return typeof(DateTime);
            }
            catch (InvalidOperationException)
            { }
            catch (JsonReaderException)
            { }

            return null;
        }

    }
}
