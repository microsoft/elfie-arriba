// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    public interface IStringStore : IStatistics, IBinarySerializable
    {
        String8 this[int identifier] { get; }
        Range RangeForString(int identifier);

        bool TryFindString(string value, out Range matches);
        bool TryFindString(String8 value, out Range matches);
        bool TryGetRangeStartingWith(String8 prefix, out Range matches);

        int CompareValues(int leftIdentifier, int rightIdentifier);

        int GetSerializationIdentifier(int identifier);
    }
}
