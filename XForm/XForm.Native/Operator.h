// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

// WARNING: Values must stay in sync with XForm.Query.Operator

public enum CompareOperatorN : char
{
	Equal = 0,
	NotEqual = 1,
	LessThan = 2,
	LessThanOrEqual = 3,
	GreaterThan = 4,
	GreaterThanOrEqual = 5
};

public enum BooleanOperatorN : char
{
	And = 0,
	Or = 1
};

public enum SigningN : char
{
	Signed = 0,
	Unsigned = 1
};