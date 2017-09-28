using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class LDFReaderTests
    {
        private const string SampleContent = @"dn: CN=Scott Louvau,OU=UserAccounts,DC=domain,DC=com
changetype: add
cn: Scott Louvau
whenCreated: 19990410024913.0Z
whenChanged: 20170922025542.0Z
pwdLastSet: 131505223204018920
objectSid:: AQUAAAAAAAUVAAAAFdoFDGWeMwFVdzMB9AEAAA==
distinguishedName: 
 CN=Scott Louvau,OU=UserAccounts,DC=domain,DC=com
memberOf: 
 CN=Interesting Group,OU=Distribution Lists,DC=domain,DC=com
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
objectSid:: AQUAAAAAAAUVAAAAF
 doFDGWeMwFVdzMB9AEAAA==";

        [TestMethod]
        public void LDFReader_Basics()
        {
            File.WriteAllText("Sample.ldf", SampleContent);

            using (ITabularReader reader = new LDFTabularReader("Sample.ldf"))
            {
                // Validate column names found
                Assert.AreEqual("dn, changetype, cn, whenCreated, whenChanged, pwdLastSet, objectSid, distinguishedName, memberOf", string.Join(", ", reader.Columns));

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
