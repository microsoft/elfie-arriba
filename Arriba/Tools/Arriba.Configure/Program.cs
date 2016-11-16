using Arriba.Model;
using Arriba.Model.Security;
using Newtonsoft.Json;
using System.IO;
using System.Xml.Serialization;

namespace Arriba.Configure
{
    class Program
    {
        static void Main(string[] args)
        {
            Sample();
        }

        private static void Secure(string tableName, string securityXmlFilePath)
        {
            SecurityPermissions security = new SecurityPermissions();

            string securityJson = File.ReadAllText(securityXmlFilePath);
            security = JsonConvert.DeserializeObject<SecurityPermissions>(securityJson);

            SecureDatabase d = new SecureDatabase();
            d.SetSecurity(tableName, security);
            d.SaveSecurity(tableName);
        }

        private static void Sample()
        {
            SecurityPermissions sample = new SecurityPermissions();
            sample.Grant(new SecurityIdentity(IdentityScope.User, "Domain\\v-scolo"), PermissionScope.Owner);

            sample.SecureColumns(new SecurityIdentity(IdentityScope.Group, "Domain\\ArribaSAD"), new string[] { "DirectAdministrators", "AllAdminUsers" });
            sample.SecureRows(new SecurityIdentity(IdentityScope.Group, "Domain\\Arriba Sensitive Xbox"), "S1=\"Xbox\"");

            File.WriteAllText("SampleSecurity.json", JsonConvert.SerializeObject(sample));
        }
    }
}
