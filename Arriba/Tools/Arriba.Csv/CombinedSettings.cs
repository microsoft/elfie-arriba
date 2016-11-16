using Arriba.Model.Column;
using Arriba.Model.Security;
using System.Collections.Generic;

namespace Arriba.Csv
{
    /// <summary>
    ///  CombinedSettings contains the different settings details for a Table
    ///  to describe a Json format for providing them.
    /// </summary>
    public class CombinedSettings
    {
        public long ItemCountLimit { get; set; }
        public SecurityPermissions Security { get; set; }
        public List<ColumnDetails> Schema { get; set; }

        public CombinedSettings()
        {
            this.Security = new SecurityPermissions();
            this.Schema = new List<ColumnDetails>();
        }
    }
}
