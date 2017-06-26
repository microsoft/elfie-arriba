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
	AndNot = 1,
	Or = 2
};

public enum SigningN : char
{
	Unsigned = 0,
	Signed = 1
};

private class CompareToVector
{
public:
	static void Where(CompareOperatorN cOp, BooleanOperatorN bOp, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);

	template<typename T>
	static void WhereSingle(CompareOperatorN cOp, BooleanOperatorN bOp, T* set, int length, T value, unsigned __int64* matchVector);
};

