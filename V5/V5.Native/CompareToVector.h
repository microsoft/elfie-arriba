#pragma once
private class CompareToVector
{
public:
	static void WhereGreaterThan(bool positive, bool and, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);
	static void WhereLessThan(bool positive, bool and, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);
	static void WhereEquals(bool positive, bool and, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);
};

