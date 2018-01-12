// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Types
{
    /// <summary>
    ///  IValueCopier makes copies of values which can't just be copied by assignment.
    ///  [String8]
    /// </summary>
    public interface IValueCopier
    {
        void Reset();
    }

    public interface IValueCopier<T> : IValueCopier
    {
        T Copy(T value);
    }
}
