// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Model
{
    /// <summary>
    ///  AddOrUpdate mode determines how AddOrUpdate handles items
    ///  with IDs not already in the table.
    ///   - AddOrUpdate adds rows for new IDs
    ///   - UpdateOnly throws for new IDs
    ///   - UpdateAndIgnoreAdds skips items with new IDs
    /// </summary>
    public enum AddOrUpdateMode : byte
    {
        AddOrUpdate = 0,
        UpdateOnly = 1,
        UpdateAndIgnoreAdds = 2
    }

    /// <summary>
    ///  AddOrUpdateOptions is used to control the behavior of Table.AddOrUpdate.
    /// </summary>
    public class AddOrUpdateOptions
    {
        public static AddOrUpdateOptions Default = new AddOrUpdateOptions();

        /// <summary>
        ///  Mode determines what to do with items with new IDs.
        ///  By default, rows are added for items with new IDs.
        /// </summary>
        public AddOrUpdateMode Mode { get; set; }

        /// <summary>
        ///  AddMissingColumns determines whether to add columns not seen before
        ///  or throw an exception if a new column name is passed.
        ///  By default, columns are not added automatically.
        /// </summary>
        public bool AddMissingColumns { get; set; }

        public AddOrUpdateOptions()
        {
            this.Mode = AddOrUpdateMode.AddOrUpdate;
            this.AddMissingColumns = false;
        }
    }
}
