// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class LDFReaderTests
    {
        private const string SampleContent = @"#Comment
dn: CN=Scott Louvau,OU=UserAccounts,DC=domain,DC=com
changetype: add
cn: Scott Louvau
whenCreated: 19990410024913.0Z
whenChanged: 20170922025542.0Z
pwdLastSet:131505223204018920
objectSid:: AQUAAAAAAAUVAAAAFdoFDGWeMwFVdzMB9AEAAA==
distinguishedName: 
 CN=Scott Louvau,OU=UserAccounts,DC=domain,DC=com
memberOf: 
 CN=Interesting Group,OU=Distribution Lists,DC=domain,DC=com
-
memberOf: 
 CN=Another Group,OU=Distribution Lists,DC=domain,DC=com
memberOf: 
 CN=Wrapped Group,OU=Specific OU,OU=Container OU
 ,OU=Resources,DC=domain,DC=com

dn: CN=Second User,OU=UserAccounts,DC=domain,DC=com
changetype: add
cn: Second User

dn: CN=Third User,OU=UserAccounts,DC=domain,DC=com
changetype: add
cn: Third User
lockoutTime: 131505223204018920
objectSid:: AQUAAAAAAAUVAAAAF
 doFDGWeMwFVdzMB9AEAAA==";

        [TestMethod]
        public void LDFReader_Basics()
        {
            File.WriteAllText("Sample.ldf", SampleContent);

            int colIndex;
            using (ITabularReader reader = new LdfTabularReader("Sample.ldf"))
            {
                // Validate column names found
                Assert.AreEqual("dn, changetype, cn, whenCreated, whenChanged, pwdLastSet, objectSid, distinguishedName, memberOf, lockoutTime", string.Join(", ", reader.Columns));

                colIndex = 0;
                Assert.IsTrue(reader.NextRow());
                Assert.AreEqual(10, reader.CurrentRowColumns);
                Assert.AreEqual("CN=Scott Louvau,OU=UserAccounts,DC=domain,DC=com", reader.Current(colIndex++).ToString());
                Assert.AreEqual("add", reader.Current(colIndex++).ToString());
                Assert.AreEqual("Scott Louvau", reader.Current(colIndex++).ToString());
                Assert.AreEqual("19990410024913.0Z", reader.Current(colIndex++).ToString());
                Assert.AreEqual("20170922025542.0Z", reader.Current(colIndex++).ToString());
                Assert.AreEqual("131505223204018920", reader.Current(colIndex++).ToString());
                Assert.AreEqual("S-1-5-21-201710101-20160101-20150101-500", reader.Current(colIndex++).ToString());
                Assert.AreEqual("CN=Scott Louvau,OU=UserAccounts,DC=domain,DC=com", reader.Current(colIndex++).ToString());
                Assert.AreEqual("CN=Interesting Group,OU=Distribution Lists,DC=domain,DC=com;CN=Another Group,OU=Distribution Lists,DC=domain,DC=com;CN=Wrapped Group,OU=Specific OU,OU=Container OU,OU=Resources,DC=domain,DC=com", reader.Current(colIndex++).ToString());
                Assert.AreEqual("", reader.Current(colIndex++).ToString());

                colIndex = 0;
                Assert.IsTrue(reader.NextRow());
                Assert.AreEqual(10, reader.CurrentRowColumns);
                Assert.AreEqual("CN=Second User,OU=UserAccounts,DC=domain,DC=com", reader.Current(colIndex++).ToString());
                Assert.AreEqual("add", reader.Current(colIndex++).ToString());
                Assert.AreEqual("Second User", reader.Current(colIndex++).ToString());
                while (colIndex < reader.CurrentRowColumns)
                {
                    Assert.AreEqual("", reader.Current(colIndex++).ToString());
                }

                colIndex = 0;
                Assert.IsTrue(reader.NextRow());
                Assert.AreEqual(10, reader.CurrentRowColumns);
                Assert.AreEqual("CN=Third User,OU=UserAccounts,DC=domain,DC=com", reader.Current(colIndex++).ToString());
                Assert.AreEqual("add", reader.Current(colIndex++).ToString());
                Assert.AreEqual("Third User", reader.Current(colIndex++).ToString());
                Assert.AreEqual("", reader.Current(colIndex++).ToString());
                Assert.AreEqual("", reader.Current(colIndex++).ToString());
                Assert.AreEqual("", reader.Current(colIndex++).ToString());
                Assert.AreEqual("S-1-5-21-201710101-20160101-20150101-500", reader.Current(colIndex++).ToString());
                Assert.AreEqual("", reader.Current(colIndex++).ToString());
                Assert.AreEqual("", reader.Current(colIndex++).ToString());
                Assert.AreEqual("131505223204018920", reader.Current(colIndex++).ToString());

                Assert.IsFalse(reader.NextRow());

                // Validate row count read
                Assert.AreEqual(3, reader.RowCountRead);
            }
        }
    }
}
