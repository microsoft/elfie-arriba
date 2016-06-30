// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  Base class for results from different query types, containing
    ///  values which should be returned for all of them uniformly.
    /// </summary>
    public class BaseResult : IBaseResult
    {
        public TimeSpan Runtime { get; set; }
        public ExecutionDetails Details { get; set; }

        public BaseResult()
        {
            this.Details = new ExecutionDetails();
        }
    }

    public interface IBaseResult
    {
        TimeSpan Runtime { get; set; }
        ExecutionDetails Details { get; set; }
    }
}
