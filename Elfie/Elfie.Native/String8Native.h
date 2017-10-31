#pragma once
using namespace System;

public ref class String8Native
{
public:
	static void ToLower(Byte* text, Int32 textLength);
	static int Compare(Byte* left, Int32 leftLength, Byte* right, Int32 rightLength);
	static int CompareOrdinalIgnoreCase(Byte* left, Int32 leftLength, Byte* right, Int32 rightLength);
	static int IndexOf(Byte* text, Int32 textIndex, Int32 textLength, Byte* value, Int32 valueLength);

	static int SplitAlphanumeric(Byte* text, Int32 startIndex, Int32 textLength, Int32* outWordBoundaries, Int32 outDelimiterLengthLimit);
	//static int Split(Byte* text, Int32 textIndex, Int32 textLength, Byte delimiter, Int32* outDelimiterIndices, Int32 outDelimiterLengthLimit);
	//static int SplitCsv(Byte* text, Int32 startIndex, Int32 textLength, bool* withinQuotes, Int32* outCellBoundaries, Int32 outCellLengthLimit);

	static void LineAndChar(Byte* text, Int32 textIndex, Int32* outLineNumber, Int32* outCharInLine);
	static void NextCopyrightComment(Byte* text, Int32 startIndex, Int32 textLength, Int32* matchStartIndex, Int32* matchEndIndex);

};

