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
		And = 0,
		AndNot = 1,
		Or = 2
	};
}