// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>

const int Utf16IndexOfMode = _SIDD_UWORD_OPS | _SIDD_CMP_EQUAL_ORDERED;
const int Utf16FirstDifferentCharacterMode = _SIDD_UWORD_OPS | _SIDD_CMP_EQUAL_EACH | _SIDD_NEGATIVE_POLARITY;

static bool EqualsShort(unsigned __int16* left, unsigned __int16* right, __int32 length)
{
	__m128i leftBlock = _mm_loadu_si128((__m128i*)(&left[0]));
	__m128i rightBlock = _mm_loadu_si128((__m128i*)(&right[0]));

	int matchOffset = _mm_cmpestri(leftBlock, length, rightBlock, length, Utf16FirstDifferentCharacterMode);
	return (matchOffset >= length);
}

extern "C" __declspec(dllexport) bool Equals(unsigned __int16* left, unsigned __int16* right, __int32 length)
{
	int i = 0;
	while (i < length - 16)
	{
		if (!EqualsShort(left + i, right + i, 16)) return false;
		i += 16;
	}

	if (i >= length) return false;
	return EqualsShort(left + i, right + i, length - i);
}

extern "C" __declspec(dllexport) __int32 IndexOf(unsigned __int16* text, __int32 textLength, unsigned __int16* value, __int32 valueLength)
{
	// Load the text we're searching for
	__m128i valueBlock = _mm_loadu_si128((__m128i*)value);
	int valueLengthToMatch = (valueLength > 8 ? 8 : valueLength);

	// Compute the last position at which a match would fit
	int lastMatchPosition = textLength - valueLength;

	// Match full blocks while 8+ characters remain to match
	int fullBlockLength = textLength - 8;
	if (fullBlockLength > lastMatchPosition) fullBlockLength = lastMatchPosition + 1;

	int i = 0;
	for (; i < fullBlockLength; i += 8)
	{
		// Load 16 bytes to scan
		__m128i textBlock = _mm_loadu_si128((__m128i*)(&text[i]));

		// Look for a possible match in the block
		int matchOffset = _mm_cmpestri(valueBlock, valueLengthToMatch, textBlock, 8, Utf16IndexOfMode);

		// If a match was found, verify the full value matches
		if (matchOffset < 8)
		{
			int matchIndex = i + matchOffset;
			if (Equals(text + matchIndex, value, valueLength)) return matchIndex;

			// If not, check the next possible character next iteration
			i = matchIndex + 1 - 8;
		}
	}

	if (i < lastMatchPosition)
	{
		// Load the last 16 bytes to scan
		__m128i textBlock = _mm_loadu_si128((__m128i*)(&text[i]));

		// Look for a possible match in the block
		int matchOffset = _mm_cmpestri(valueBlock, valueLengthToMatch, textBlock, textLength - i, Utf16IndexOfMode);

		// If a match was found, verify the full value matches
		int matchIndex = i + matchOffset;
		if (matchIndex <= lastMatchPosition) return matchIndex;
	}

	return -1;
}
