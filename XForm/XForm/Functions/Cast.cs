// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Columns;
using XForm.Data;
using XForm.Types;

namespace XForm.Functions
{
    internal class CastBuilder : IFunctionBuilder
    {
        public string Name => "Cast";
        public string Usage => "Cast({Col|Func|Const}, {ToType}, {ErrorOn?}, {DefaultValue?}, {DefaultOn?})";
        public Type ReturnType => null;

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            // Column and ToType are required
            IXColumn column = context.Parser.NextColumn(source, context);
            Type toType = context.Parser.NextType();

            // ErrorOn, DefaultValue, and ChangeToDefaultOn are optional
            ValueKinds errorOn = ValueKindsDefaults.ErrorOn;
            object defaultValue = null;
            ValueKinds changeToDefaultOn = ValueKindsDefaults.ChangeToDefault;

            if (context.Parser.HasAnotherArgument)
            {
                // Parse ErrorOn if there's another argument
                errorOn = context.Parser.NextEnum<ValueKinds>();

                // If there's another argument, both of the last two are required
                if (context.Parser.HasAnotherArgument)
                {
                    defaultValue = context.Parser.NextLiteralValue();
                    changeToDefaultOn = context.Parser.NextEnum<ValueKinds>();
                }
            }

            return CastedColumn.Build(source, column, toType, errorOn, defaultValue, changeToDefaultOn);
        }
    }
}
