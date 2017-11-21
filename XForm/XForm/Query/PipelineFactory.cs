// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using XForm.Data;
using XForm.Query;

namespace XForm
{
    public class PipelineFactory
    {
        public static IDataBatchEnumerator BuildPipeline(string xqlQuery, IDataBatchEnumerator pipeline = null)
        {
            return PipelineParser.BuildPipeline(pipeline, xqlQuery);
        }

        public static IDataBatchEnumerator BuildStage(string xqlLine, IDataBatchEnumerator source)
        {
            return PipelineParser.BuildStage(source, xqlLine);
        }
    }
}
