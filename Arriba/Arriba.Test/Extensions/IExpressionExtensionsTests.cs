// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Model.Expressions;
using Arriba.Model.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Extensions
{
    [TestClass]
    public class IExpressionExtensionsTests
    {
        [TestMethod]
        public void GetAllTerms_Basic()
        {
            IExpression sample = QueryParser.Parse("everything && Title:hello && ID < 394000 && (Title:\"Quoted Value\" || !(Status = \"Active\"))");

            Assert.AreEqual("[*]:everything|[Title]:hello|[ID] < 394000|[Title]:\"Quoted Value\"|[Status] = Active", String.Join("|", sample.GetAllTerms(null)));
            Assert.AreEqual("[*]:everything|[Title]:hello|[Title]:\"Quoted Value\"", String.Join("|", sample.GetAllTerms("Title")));
            Assert.AreEqual("[*]:everything|[Status] = Active", String.Join("|", sample.GetAllTerms("status")));
            Assert.AreEqual("[*]:everything", String.Join("|", sample.GetAllTerms("Missing")));
        }
    }
}
