#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "String8N.h"

#pragma unmanaged
const int Utf8IndexOfMode = _SIDD_UBYTE_OPS | _SIDD_CMP_EQUAL_ORDERED;
const int Utf8FirstDifferentCharacterMode = _SIDD_UBYTE_OPS | _SIDD_CMP_EQUAL_EACH | _SIDD_NEGATIVE_POLARITY;
const int Utf8RangeMaskMode = _SIDD_UBYTE_OPS | _SIDD_CMP_RANGES | _SIDD_UNIT_MASK;

const __m128i uppercaseRange = { 'A', 'Z' };
const __m128i caseConvert = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

static int SplitTsvN(unsigned __int8* content, int contentIndex, int contentEnd, unsigned __int64* cellVector, unsigned __int64* rowVector)
{
	// TODO: Fill only one vector (cells) and return rowCount.
	// Properly handle uneven last < 64 characters.

	int rowCount = 0;

	// Load vectors of the delimiters we're looking for
	__m256i newline = _mm256_set1_epi8('\n');
	__m256i tab = _mm256_set1_epi8('\t');

	int index = contentIndex;
	for (; index < contentEnd; index += 64)
	{
		// Load 64 bytes to scan
		__m256i block1 = _mm256_loadu_si256((__m256i*)(&content[index]));
		__m256i block2 = _mm256_loadu_si256((__m256i*)(&content[index + 32]));

		// Find all tabs and newlines and build bit vectors of them
		unsigned int tabs1 = _mm256_movemask_epi8(_mm256_cmpeq_epi8(block1, tab));
		unsigned int tabs2 = _mm256_movemask_epi8(_mm256_cmpeq_epi8(block2, tab));
		unsigned int lines1 = _mm256_movemask_epi8(_mm256_cmpeq_epi8(block1, newline));
		unsigned int lines2 = _mm256_movemask_epi8(_mm256_cmpeq_epi8(block2, newline));

		unsigned __int64 lines = ((unsigned __int64)lines2 << 32) | lines1;
		unsigned __int64 cells = ((unsigned __int64)tabs2 << 32) | tabs1 | lines;

		// Cells are every tab or line and Rows are every line
		cellVector[index >> 6] = cells;
		rowVector[index >> 6] = lines;

		// Count lines
		rowCount += (int)_mm_popcnt_u64(lines);
	}

	// Match remaining values individually

	return rowCount;
}

extern "C" __declspec(dllexport) int CompareInternal(Byte* left, Int32 leftLength, Byte* right, Int32 rightLength)
{
	if (leftLength == 0 && rightLength == 0) return 0;
	if (leftLength == 0) return -1;
	if (rightLength == 0) return 1;

	int length = (leftLength < rightLength ? leftLength : rightLength);

	int i = 0;
	int fullBlockLength = length - 15;

	int matchOffset = 0;
	for (i = 0; i < fullBlockLength; i += 16)
	{
		__m128i leftBlock = _mm_loadu_si128((__m128i*)(&left[i]));
		__m128i rightBlock = _mm_loadu_si128((__m128i*)(&right[i]));
		matchOffset = _mm_cmpestri(leftBlock, 16, rightBlock, 16, Utf8FirstDifferentCharacterMode);

		if (matchOffset != 16) break;
	}

	if (i >= fullBlockLength && i < length)
	{
		int lengthLeft = length - i;
		__m128i leftBlock = _mm_loadu_si128((__m128i*)(&left[i]));
		__m128i rightBlock = _mm_loadu_si128((__m128i*)(&right[i]));
		matchOffset = _mm_cmpestri(leftBlock, lengthLeft, rightBlock, lengthLeft, Utf8FirstDifferentCharacterMode);
	}

	// If the strings were equal, the longer one is later
	if (i + matchOffset >= length)
	{
		return leftLength - rightLength;
	}

	// Otherwise, compare the first non-equal byte
	return left[i + matchOffset] - right[i + matchOffset];
}


extern "C" __declspec(dllexport) int IndexOfAllInternal(Byte* text, Int32 textIndex, Int32 textLength, Byte* value, Int32 valueLength, Int32* result, Int32 resultLimit)
{
	// TODO: Figure out why it's not finding all matches.
	// Make second compare more efficient (set next loop to match at first index, add matches at first index?)
	// Is this faster if case sensitive?
	// Profile to see runtime spent here vs. finding row of match.

	int resultCount = 0;

	__m128i uppercaseMask;
	__m128i corrector;

	// Load the text we're searching for
	__m128i searchForBlock = _mm_loadu_si128((__m128i*)(&value[0]));
	uppercaseMask = _mm_cmpistrm(uppercaseRange, searchForBlock, Utf8RangeMaskMode);
	corrector = _mm_and_si128(uppercaseMask, caseConvert);
	searchForBlock = _mm_xor_si128(searchForBlock, corrector);
	
	// Compute the last position at which a match would fit
	int lastMatchPosition = textLength - valueLength;

	// Match full blocks while 16+ characters remain to match
	int fullBlockLength = textLength - 15;
	if (fullBlockLength > lastMatchPosition) fullBlockLength = lastMatchPosition + 1;

	// If a match is found before this index, a second comparison isn't needed
	int isFullyMatchedAtIndex = 16 - valueLength;

	int i;
	for (i = textIndex; i < fullBlockLength; i += 16)
	{
		// Load 16 bytes to scan
		__m128i textBlock = _mm_loadu_si128((__m128i*)(&text[i]));

		// Left ToLowerInvariant
		uppercaseMask = _mm_cmpistrm(uppercaseRange, textBlock, Utf8RangeMaskMode);
		corrector = _mm_and_si128(uppercaseMask, caseConvert);
		textBlock = _mm_xor_si128(textBlock, corrector);

		// Look for searchFor with cmp*i*stri [performance]
		int matchOffset = _mm_cmpistri(searchForBlock, textBlock, Utf8IndexOfMode);

		if (matchOffset < 16)
		{
			int matchIndex = i + matchOffset;
			if (matchOffset <= isFullyMatchedAtIndex || CompareInternal(text + matchIndex, valueLength, value, valueLength) == 0)
			{
				result[resultCount++] = matchIndex;
				if (resultCount == resultLimit) return resultCount;
			}

			// Look at the next possible character next iteration
			i = matchIndex + 1 - 16;
		}
	}

	
	while(i < textLength)
	{
		int lengthLeft = textLength - i;
		__m128i textBlock = _mm_loadu_si128((__m128i*)(&text[i]));

		// Left ToLowerInvariant
		uppercaseMask = _mm_cmpistrm(uppercaseRange, textBlock, Utf8RangeMaskMode);
		corrector = _mm_and_si128(uppercaseMask, caseConvert);
		textBlock = _mm_xor_si128(textBlock, corrector);

		int matchOffset = _mm_cmpestri(searchForBlock, valueLength, textBlock, lengthLeft, Utf8IndexOfMode);
		if (matchOffset <= isFullyMatchedAtIndex)
		{
			int matchIndex = i + matchOffset;
			result[resultCount++] = i + matchOffset;
			if (resultCount == resultLimit) return resultCount;

			i = matchIndex + 1;
		}
		else
		{
			break;
		}
	}

	return resultCount;
}

#pragma managed

namespace XForm
{
	namespace Native
	{
		Int32 String8N::SplitTsv(array<Byte>^ content, Int32 index, Int32 length, array<UInt64>^ cellVector, array<UInt64>^ rowVector)
		{
			pin_ptr<Byte> pContent = &content[0];
			pin_ptr<UInt64> pCellVector = &cellVector[0];
			pin_ptr<UInt64> pRowVector = &rowVector[0];
			return SplitTsvN(pContent, index, index + length, pCellVector, pRowVector);
		}

		Int32 String8N::IndexOfAll(array<Byte>^ content, Int32 index, Int32 length, array<Byte>^ value, Int32 valueIndex, Int32 valueLength, array<Int32>^ matchArray)
		{
			pin_ptr<Byte> pContent = &content[0];
			pin_ptr<Byte> pValue = &value[valueIndex];
			pin_ptr<Int32> pMatchArray = &matchArray[0];

			return IndexOfAllInternal(pContent, index, index + length, pValue, valueLength, pMatchArray, matchArray->Length);
		}
	}
}
