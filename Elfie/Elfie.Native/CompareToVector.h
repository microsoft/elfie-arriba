#pragma once
private class CompareToVector
{
public:
	// Single Byte Comparison using SIMD instructions
	static void Where(CompareOperatorN cOp, BooleanOperatorN bOp, SigningN signing, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);

	// Two Byte Comparison using SIMD instructions
	static void Where(CompareOperatorN cOp, BooleanOperatorN bOp, SigningN signing, unsigned __int16* set, int length, unsigned __int16 value, unsigned __int64* matchVector);

	template<typename T>
	static void WhereSingle(CompareOperatorN cOp, BooleanOperatorN bOp, T* set, int length, T value, unsigned __int64* matchVector);
};

