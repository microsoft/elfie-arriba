// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class IISLogReaderTests
    {
        private const string SampleContent = @"#Software: Microsoft Internet Information Services 8.5
#Version: 1.0
#Date: 2017-05-13 00:28:25
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
2017-05-13 00:28:25 10.10.1.1 GET /allBasics - 42785 - 10.10.1.2 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/52.0.2743.116+Safari/537.36+Edge/15.15063 https://arriba/ 401 2 5 453
2017-05-13 00:28:26 10.10.1.1 GET /suggest q=Q1 42785 - 10.10.1.2 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/52.0.2743.116+Safari/537.36+Edge/15.15063 https://arriba/ 401 2 5 15
2017-05-13 00:28:32 10.10.1.1 GET /suggest q=Q1 42785 DOMAIN\user1 10.10.1.2 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/52.0.2743.116+Safari/537.36+Edge/15.15063 https://arriba/ 200 0 0 5484
2017-05-13 00:28:32 10.10.1.1 GET /allBasics - 42785 DOMAIN\user1 10.10.1.2 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/52.0.2743.116+Safari/537.36+Edge/15.15063 https://arriba/ 200 0 0 6437
#Software: Microsoft Internet Information Services 8.5
#Version: 1.0
#Date: 2017-05-13 01:06:31
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
2017-05-13 01:06:30 10.10.1.1 GET /allBasics - 42785 - 10.10.1.3 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/52.0.2743.116+Safari/537.36+Edge/15.15063 https://arriba/ 401 2 5 406
2017-05-13 01:06:38 10.10.1.1 GET /allBasics - 42785 DOMAIN\user2 10.10.1.3 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/52.0.2743.116+Safari/537.36+Edge/15.15063 https://arriba/ 200 0 0 6516
#Software: Microsoft Internet Information Services 8.5
#Version: 1.0
#Date: 2017-05-13 02:46:47
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
2017-05-13 02:46:47 10.10.1.1 GET /allBasics - 42785 - 10.10.1.4 Mozilla/4.0+(compatible;+MSIE+7.0;+Windows+NT+10.0;+WOW64;+Trident/7.0;+Touch;+.NET4.0C;+.NET4.0E;+.NET+CLR+2.0.50727;+.NET+CLR+3.0.30729;+.NET+CLR+3.5.30729;+Tablet+PC+2.0;+InfoPath.3) https://arriba/ 401 2 5 968";

        [TestMethod]
        public void IISLogReader_Basics()
        {
            File.WriteAllText("Sample.iislog", SampleContent);

            using (ITabularReader reader = new IISTabularReader("Sample.iislog"))
            {
                // Validate column names found
                Assert.AreEqual("date, time, s-ip, cs-method, cs-uri-stem, cs-uri-query, s-port, cs-username, c-ip, cs(User-Agent), cs(Referer), sc-status, sc-substatus, sc-win32-status, time-taken", string.Join(", ", reader.Columns));

                while (reader.NextRow())
                {
                    // Spot Check values
                    Assert.AreEqual("2017-05-13", reader.Current(0).ToString());
                    Assert.AreEqual("10.10.1.1", reader.Current(2).ToString());
                    Assert.AreEqual("GET", reader.Current(3).ToString());
                    Assert.AreEqual("https://arriba/", reader.Current(10).ToString());
                }

                // Validate row count read
                Assert.AreEqual(7, reader.RowCountRead);
            }
        }
    }
}
