// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.PDB;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.Sdk;
using SarifSdk = Microsoft.CodeAnalysis.Sarif.Sdk;
using Microsoft.CodeAnalysis.Sarif.Writers;

using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Elfie.Search.Sarif
{
    internal class SarifConverter
    {
        private const string ELFIE_RULEID = "ELF1001";

        public static void ConvertToSarif(PartialArray<Symbol> queryResults, MemberQuery query, string outputPath)
        {
            ResultLog resultLog = BuildCoreResultLog();

            var sarifResults = resultLog.RunLogs[0].Results;

            foreach (Symbol match in queryResults)
            {
                if (!match.HasLocation) { continue; }
                sarifResults.Add(ConvertToSarifResult(match, query));
            }

            // Note: we'll produce indented JSON as a convenience
            // for now to assist in examining generated file
            // while it's under churn.
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ContractResolver = SarifContractResolver.Instance,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            File.WriteAllText(outputPath, JsonConvert.SerializeObject(resultLog, settings));
        }

        private static ResultLog BuildCoreResultLog()
        {
            var resultLog = new ResultLog();
            resultLog.Version = SarifVersion.ZeroDotFour;

            resultLog.RunLogs = new List<RunLog>();

            var runLog = new RunLog()
            {
                ToolInfo = new ToolInfo()
                {
                    Name = "Elfie"
                },
                Results = new List<Result>(),
                RuleInfo = new[]
                    {
                        new RuleDescriptor()
                        {
                            Id = ELFIE_RULEID,
                            HelpUri = new Uri("https://github.com/michaelcfanning/elfie-private"),
                            Properties = new Dictionary<string, string>(),
                            ShortDescription = "Extensible Lightweight Fast Indexing of Entities (ELFIE)",
                            FullDescription = "Extensible Lightweight Fast Indexing of Entities (ELFIE). Elfie " +
                                              "is a fast, extensible indexing mechanism that provides search over " +
                                              "code and code-related metadata.",
                            Name = "Search result",
                            FormatSpecifiers = new Dictionary<string, string>()
                        }
                    }
            };

            runLog.RuleInfo[0].Properties["Category"] = "Search";
            runLog.RuleInfo[0].FormatSpecifiers["Default"] = "Query: '{0}' Match: '{1}'";

            resultLog.RunLogs.Add(runLog);

            return resultLog;
        }

        private static Result ConvertToSarifResult(Symbol match, MemberQuery query)
        {
            // TODO express matches against logical 
            // locations with no file location
            Debug.Assert(match.HasLocation);

            string localPath = SourceFileMap.GetLocalPath(match);
            var region = new Region()
            {
                StartLine = match.Line,
                StartColumn = match.CharInLine
            };

            string shortMessage = match.FullName.ToString();
            string fullMessage = string.Format(
                "query:'{0}' match: '{1}'",
                query.SymbolName,
                shortMessage);

            var result = new Result()
            {
                RuleId = "ELF1001", // Note there is an active SARIF issue (#76)
                                    // open to make id optional for notes.
                Kind = ResultKind.Note,
                ShortMessage = shortMessage,
                FormattedMessage = new FormattedMessage()
                {
                    SpecifierId = "Default",
                    Arguments = new List<string>(new[] { query.SymbolName, match.FullName.ToString() })
                },
                Locations = new List<SarifSdk.Location>
                 {
                     new SarifSdk.Location()
                     {
                         ResultFile = new[]
                         {
                             new PhysicalLocationComponent()
                             {
                                  Uri = localPath.CreateUriForJsonSerialization(),
                                  MimeType = MimeType.DetermineFromFileExtension(localPath),
                                  Region = region
                             }
                         },
                         FullyQualifiedLogicalName = match.FullName.ToString(),
                         LogicalLocation = BuildLogicalLocation(match)
                         // TODO: add assembly FILE NAME as AnalysisTarget
                     }
                 }
            };
            return result;
        }

        private static IList<LogicalLocationComponent> BuildLogicalLocation(Symbol match)
        {
            var result = new List<LogicalLocationComponent>();

            Stack<Symbol> symbolStack = new Stack<Symbol>();

            Symbol current = match;

            while (!current.Equals(default(Symbol)))
            {
                symbolStack.Push(current);
                current = current.Parent();
            }

            while (symbolStack.Count > 0)
            {
                current = symbolStack.Pop();
                result.Add(new LogicalLocationComponent()
                {
                    Kind = current.Type.ToString(),
                    Name = current.Name.ToString()
                });
            }

            return result;
        }
    }
}
