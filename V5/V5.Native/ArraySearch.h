#pragma once
using namespace System;

public ref class ArraySearch
{
public:	
	static void WhereGreaterThan(array<Byte>^ set, SByte value, array<UInt32>^ matchVector);

	static int Count(array<UInt32>^ matchVector);
};

