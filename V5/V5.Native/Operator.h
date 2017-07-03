#pragma once

namespace V5
{
	public enum class CompareOperator : char
	{
		Equals = 0,
		NotEquals = 1,
		LessThan = 2,
		LessThanOrEqual = 3,
		GreaterThan = 4,
		GreaterThanOrEqual = 5
	};

	public enum class BooleanOperator : char
	{
		Set = 0,
		And = 1,
		AndNot = 2,
		Or = 3
	};
}

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
	Set = 0,
	And = 1,
	AndNot = 2,
	Or = 3
};

public enum SigningN : char
{
	Unsigned = 0,
	Signed = 1
};