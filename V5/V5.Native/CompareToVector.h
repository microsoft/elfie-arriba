#pragma once
private class CompareToVector
{
public:
	static void AndWhereGreaterThan(bool positive, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);
	static void AndWhereLessThan(bool positive, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);
	static void AndWhereEquals(bool positive, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector);
};

