#pragma once

public enum CompareOperatorN : char
{
	Equals = 0,
	NotEquals = 1,
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