// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  Position tracks the Console Window cursor position and can clear output
    ///  between positions.
    /// </summary>
    public class Position
    {
        public int Left;
        public int Top;

        public Position()
        {
            Save();
        }

        public void Save()
        {
            Left = Console.CursorLeft;
            Top = Console.CursorTop;
        }

        public void Restore()
        {
            Console.SetCursorPosition(Left, Top);
        }

        public void ClearUpTo(Position end)
        {
            // Clear the rest of the start line
            StringBuilder clearString = new StringBuilder();

            if (Left < Console.BufferWidth) clearString.Append(' ', Console.BufferWidth - Left);

            // Clear lines up to 'Last'
            if (Top < end.Top)
            {
                for (int i = Top; i < end.Top; ++i)
                {
                    clearString.Append(' ', Console.BufferWidth);
                }
            }

            // Move to the start
            Restore();

            // Clear in one call
            Console.Write(clearString.ToString());

            // Move back to the start
            Restore();
        }

        public int CompareTo(Position other)
        {
            int cmp = Top.CompareTo(other.Top);
            if (cmp != 0) return cmp;

            return Left.CompareTo(other.Left);
        }
    }
}
