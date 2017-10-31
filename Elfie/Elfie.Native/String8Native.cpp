#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "String8Native.h"

/*
  String functions implemented using SSE 4.2 string instruction intrinsics.
   The string instructions can search 16 bytes at a time, configured as 16 UTF8 characters
   or 8 UTF16 characters.

   The instructions are _mm_cmpestri, _mm_cmpestrm, _mm_cmpistri, and _mm_cmpistrm.
   Instructions ending in 'i' return the index of the first or last matching character.
   Instructions ending in 'm' return a mask indicating which characters matched.
   The 'cmpE' instructions take an explicit length indicating how much of the string content is valid.
   The 'cmpI' instructions look for a null character to mark the end of the valid content.

   The 'cmpE' instructions take four clock cycles to execute.
   The 'cmpI' instructions take three clock cycles to execute.

   The instructions take a "mode" which configures them.
	* _SIDD_UBYTE_OPS means each byte is a character. [UTF8]
	* _SIDD_UWORD_OPS means each two bytes are a character. [UTF16]

	* _SIDD_CMP_EQUAL_EACH means look for an exact match of the two strings. [Compare]
	* _SIDD_CMP_EQUAL_ORDERED means look for a match of one string within the other. [IndexOf]
	* _SIDD_CMP_EQUAL_ANY means look for any of the given individual characters. [String.IndexOf(char)]
	* _SIDD_CMP_RANGES means look for characters in any of the ranges given by pairs of characters. [Char.IsUpper, ...]

	* _SIDD_UNIT_MASK means make the whole byte or two bytes one bits for matches (rather than one bit per character).
	* _SIDD_NEGATIVE_POLARITY means to return the first non-match, rather than the first match, or to invert the mask returned.

	Notes:
	It's faster to make a loop to only match whole blocks and then check the last partial block outside the loop.
	If the text to searched won't contain nulls, it's faster to use the implicit instruction (cmpistri) in the loop, since the loop will only be used for full blocks anyway.

	Uses:
	ToLowerInvariant can be implemented by checking a block at a time against the range A-Z in byte mask mode.
	The result can be ANDed with 0x20 repeated and then XORed with the text to 'flip' all uppercase characters in the block.

	Compare can be implemented by comparing two blocks in EQUAL_EACH mode and looking for the first non-match with NEGATIVE_POLARITY.

	IndexOf can be implemented by comparing a block to the beginning of the value to find in EQUAL_ORDERED mode.
	Note that the instructions will return the index of a partial match at the end of the block. [If the block ends with "ed" and we're looking for "edit", we'll get a match]
	This means we can search a full block at a time, and need to do an equals afterward to see if the whole value matches in the string.

	Splitting can be implemented by comparing a block in EQUAL_ANY mode and recording the index of each match, resuming
	matches at the character after the match found.

*/

#pragma unmanaged

const int Utf8IndexOfMode = _SIDD_UBYTE_OPS | _SIDD_CMP_EQUAL_ORDERED;
const int Utf8FirstDifferentCharacterMode = _SIDD_UBYTE_OPS | _SIDD_CMP_EQUAL_EACH | _SIDD_NEGATIVE_POLARITY;
const int Utf8RangeMode = _SIDD_UBYTE_OPS | _SIDD_CMP_RANGES;
const int Utf8RangeMaskMode = _SIDD_UBYTE_OPS | _SIDD_CMP_RANGES | _SIDD_UNIT_MASK;
const __m128i alphanumericRange = { 'A', 'Z', 'a', 'z', '0', '9' };
const __m128i uppercaseRange = { 'A', 'Z' };
const __m128i caseConvert = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
const __m128i newline = { '\n' };

const char ToLowerSubtract = 'A' - 'a';

extern "C" __declspec(dllexport) void ToLowerInternal(Byte* text, Int32 textLength)
{
	int fullBlockLength = textLength - 15;
	int i;
	for (i = 0; i < fullBlockLength; i += 16)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&text[i]));
		__m128i uppercaseMask = _mm_cmpistrm(uppercaseRange, block, Utf8RangeMaskMode);
		__m128i corrector = _mm_and_si128(uppercaseMask, caseConvert);
		__m128i lowercaseBlock = _mm_xor_si128(block, corrector);
		_mm_storeu_si128((__m128i*)(&text[i]), lowercaseBlock);
	}

	if (i < textLength)
	{
		int lengthLeft = textLength - i;

		__m128i block = _mm_loadu_si128((__m128i*)(&text[i]));
		__m128i uppercaseMask = _mm_cmpestrm(uppercaseRange, 2, block, lengthLeft, Utf8RangeMaskMode);
		__m128i corrector = _mm_and_si128(uppercaseMask, caseConvert);
		__m128i lowercaseBlock = _mm_xor_si128(block, corrector);
		_mm_storeu_si128((__m128i*)(&text[i]), lowercaseBlock);
	}
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

extern "C" __declspec(dllexport) int CompareOrdinalIgnoreCaseInternal(Byte* left, Int32 leftLength, Byte* right, Int32 rightLength)
{
	if (leftLength == 0 && rightLength == 0) return 0;
	if (leftLength == 0) return -1;
	if (rightLength == 0) return 1;

	int length = (leftLength < rightLength ? leftLength : rightLength);

	int i = 0;
	int fullBlockLength = length - 15;

	__m128i leftBlock;
	__m128i rightBlock;
	__m128i uppercaseMask;
	__m128i corrector;

	int matchOffset = 0;
	for (i = 0; i < fullBlockLength; i += 16)
	{
		// Load Left
		leftBlock = _mm_loadu_si128((__m128i*)(&left[i]));

		// Left ToLowerInvariant
		uppercaseMask = _mm_cmpistrm(uppercaseRange, leftBlock, Utf8RangeMaskMode);
		corrector = _mm_and_si128(uppercaseMask, caseConvert);
		leftBlock = _mm_xor_si128(leftBlock, corrector);

		// Load Right
		rightBlock = _mm_loadu_si128((__m128i*)(&right[i]));

		// Right ToLowerInvariant
		uppercaseMask = _mm_cmpistrm(uppercaseRange, rightBlock, Utf8RangeMaskMode);
		corrector = _mm_and_si128(uppercaseMask, caseConvert);
		rightBlock = _mm_xor_si128(rightBlock, corrector);

		// Compare
		matchOffset = _mm_cmpestri(leftBlock, 16, rightBlock, 16, Utf8FirstDifferentCharacterMode);

		if (matchOffset != 16) break;
	}

	if (i >= fullBlockLength && i < length)
	{
		int lengthLeft = length - i;
		leftBlock = _mm_loadu_si128((__m128i*)(&left[i]));
		uppercaseMask = _mm_cmpistrm(uppercaseRange, leftBlock, Utf8RangeMaskMode);
		corrector = _mm_and_si128(uppercaseMask, caseConvert);
		leftBlock = _mm_xor_si128(leftBlock, corrector);

		rightBlock = _mm_loadu_si128((__m128i*)(&right[i]));
		uppercaseMask = _mm_cmpistrm(uppercaseRange, rightBlock, Utf8RangeMaskMode);
		corrector = _mm_and_si128(uppercaseMask, caseConvert);
		rightBlock = _mm_xor_si128(rightBlock, corrector);

		matchOffset = _mm_cmpestri(leftBlock, lengthLeft, rightBlock, lengthLeft, Utf8FirstDifferentCharacterMode);
	}

	// If the strings were equal, the longer one is later
	if (i + matchOffset >= length)
	{
		return leftLength - rightLength;
	}

	// Otherwise, compare the first non-equal byte
	char leftLower = left[i + matchOffset];
	if (leftLower - 'a' < 26) leftLower -= ToLowerSubtract;

	char rightLower = right[i + matchOffset];
	if (rightLower - 'a' < 26) rightLower -= ToLowerSubtract;
	return leftLower - rightLower;
}

extern "C" __declspec(dllexport) int IndexOfInternal(Byte* text, Int32 textIndex, Int32 textLength, Byte* value, Int32 valueLength)
{
	// Load the text we're searching for
	__m128i searchForBlock = _mm_loadu_si128((__m128i*)(&value[0]));

	// Compute the last position at which a match would fit
	int lastMatchPosition = textLength - valueLength;

	// Match full blocks while 16+ characters remain to match
	int fullBlockLength = textLength - 15;
	if (fullBlockLength > lastMatchPosition) fullBlockLength = lastMatchPosition + 1;

	int i;
	for (i = textIndex; i < fullBlockLength; i += 16)
	{
		// Load 16 bytes to scan
		__m128i textBlock = _mm_loadu_si128((__m128i*)(&text[i]));

		// Look for searchFor with cmp*i*stri [performance]
		int matchOffset = _mm_cmpistri(searchForBlock, textBlock, Utf8IndexOfMode);

		if (matchOffset < 16)
		{
			if (CompareInternal(text + i + matchOffset, valueLength, value, valueLength) == 0)
			{
				return i + matchOffset;
			}
			else
			{
				i += matchOffset + 1 - 16;
			}
		}
	}

	int lengthLeft = textLength - i;
	if (lengthLeft >= valueLength)
	{
		__m128i textBlock = _mm_loadu_si128((__m128i*)(&text[i]));
		int matchOffset = _mm_cmpestri(searchForBlock, valueLength, textBlock, lengthLeft, Utf8IndexOfMode);
		if (matchOffset <= lengthLeft) return i + matchOffset;
	}

	return -1;
}

extern "C" __declspec(dllexport) void LineAndCharInternal(Byte* text, Int32 textIndex, Int32* outLineNumber, Int32* outCharInLine)
{
	int lineNumber = 1;
	int lastNewlineIndex = -1;

	int fullBlockLength = textIndex - 15;
	int i;
	for (i = 0; i < fullBlockLength; i += 16)
	{
		// Load a block from the text
		__m128i withinBlock = _mm_loadu_si128((__m128i*)(&text[i]));

		// Look for and count newlines [cmpistri ~10% faster than cmpestri]
		int firstNewlineIndex = _mm_cmpistri(newline, withinBlock, Utf8IndexOfMode);

		if (firstNewlineIndex < 16)
		{
			lineNumber++;
			lastNewlineIndex = i + firstNewlineIndex;

			// Resume looking for newlines in the current block at the next character
			i += firstNewlineIndex + 1 - 16;
		}
	}

	for (; i < textIndex; ++i)
	{
		if (text[i] == '\n')
		{
			lineNumber++;
			lastNewlineIndex = i;
		}
	}

	*outLineNumber = lineNumber;
	*outCharInLine = textIndex - lastNewlineIndex;
}

const __m128i comment = { '/', '/' };
const int commentLength = 2;
const __m128i copyright = { 'C', 'o', 'p', 'y', 'r', 'i', 'g', 'h', 't' };
const int copyrightLength = 9;

extern "C" __declspec(dllexport) void NextCopyrightCommentInternal(Byte* text, Int32 startIndex, Int32 textLength, Int32* matchStartIndex, Int32* matchEndIndex)
{
	int index = startIndex;
	int fullMatchLength = 16 - commentLength + 1;

	__m128i textBlock;
	while (index < textLength)
	{
		// Look for a comment start
		int matchIndex = 0;
		for (; index < textLength; index += fullMatchLength)
		{
			textBlock = _mm_loadu_si128((__m128i*)(&text[index]));
			int commentIndex = _mm_cmpistri(comment, textBlock, Utf8IndexOfMode);
			if (commentIndex <= fullMatchLength)
			{
				// If a comment is found, start looking for the rest just after the characters were found
				matchIndex = index + commentIndex;
				index += commentIndex + commentLength;
				break;
			}
		}

		// Look for either "Copyright" (match) or a newline (no match)
		for (; index < textLength; index += 16)
		{
			textBlock = _mm_loadu_si128((__m128i*)(&text[index]));

			int newlineIndex = _mm_cmpistri(newline, textBlock, Utf8IndexOfMode);
			int copyrightIndex = _mm_cmpistri(copyright, textBlock, Utf8IndexOfMode);

			if (copyrightIndex < 16)
			{
				if (newlineIndex < copyrightIndex)
				{
					// Newline before copyright. Start looking again, after newline.
					//index = matchIndex + 1;
					index += newlineIndex + 1;
					break;
				}

				textBlock = _mm_loadu_si128((__m128i*)&text[index + copyrightIndex]);
				int copyrightMatch = _mm_cmpistri(copyright, textBlock, Utf8FirstDifferentCharacterMode);

				if (copyrightMatch >= copyrightLength)
				{
					*matchStartIndex = matchIndex;
					*matchEndIndex = index + copyrightIndex + copyrightLength;
					return;
				}
				else
				{
					// Continue looking for "Copyright"
					index += copyrightIndex + 1 - 16;
					continue;
				}
			}

			if (newlineIndex < 16)
			{
				// Newline before "copyright". Start looking again, after newline
				//index = matchIndex + 1;
				index += newlineIndex + 1;
				break;
			}
		}
	}

	// If no matches found, return -1, -1
	*matchStartIndex = -1;
	*matchEndIndex = -1;
}

extern "C" __declspec(dllexport) int SplitAlphanumericInternal(Byte* text, Int32 startIndex, Int32 textLength, Int32* outWordBoundaries, Int32 outDelimiterLengthLimit)
{
	int boundariesFound = 0;
	int index = startIndex;
	__m128i textBlock;

	while (index < textLength)
	{
		// Find the first alphanumeric character
		for (; index < textLength; index += 16)
		{
			textBlock = _mm_loadu_si128((__m128i*)(&text[index]));
			int wordStartOffset = _mm_cmpistri(alphanumericRange, textBlock, Utf8RangeMode);
			if (wordStartOffset < 16)
			{
				index += wordStartOffset + 1;
				outWordBoundaries[boundariesFound] = index - 1;
				boundariesFound++;
				break;
			}
		}

		// Find the first non-alphanumeric character
		for (; index < textLength; index += 16)
		{
			textBlock = _mm_loadu_si128((__m128i*)(&text[index]));
			int wordEndOffset = _mm_cmpistri(alphanumericRange, textBlock, Utf8RangeMode | _SIDD_NEGATIVE_POLARITY);
			if (wordEndOffset < 16)
			{
				index += wordEndOffset + 1;
				outWordBoundaries[boundariesFound] = index - 1;
				boundariesFound++;
				break;
			}
		}

		// If our array for word boundaries is full, stop searching
		if (boundariesFound == outDelimiterLengthLimit) break;
	}

	// If the string ended within a word, the end of the text is the end of the word
	if (boundariesFound % 2 == 1)
	{
		outWordBoundaries[boundariesFound] = textLength - 1;
		boundariesFound++;
	}

	return boundariesFound;
}

#pragma managed

void String8Native::ToLower(Byte* text, Int32 textLength)
{
	return ToLowerInternal(text, textLength);
}

int String8Native::Compare(Byte* left, Int32 leftLength, Byte* right, Int32 rightLength)
{
	return CompareInternal(left, leftLength, right, rightLength);
}

int String8Native::CompareOrdinalIgnoreCase(Byte* left, Int32 leftLength, Byte* right, Int32 rightLength)
{
	return CompareOrdinalIgnoreCaseInternal(left, leftLength, right, rightLength);
}

int String8Native::IndexOf(Byte* text, Int32 textIndex, Int32 textLength, Byte* value, Int32 valueLength)
{
	return IndexOfInternal(text, textIndex, textLength, value, valueLength);
}

void String8Native::LineAndChar(Byte* text, Int32 textIndex, Int32* outLineNumber, Int32* outCharInLine)
{
	LineAndCharInternal(text, textIndex, outLineNumber, outCharInLine);
}

void String8Native::NextCopyrightComment(Byte* text, Int32 startIndex, Int32 textLength, Int32* matchStartIndex, Int32* matchEndIndex)
{
	NextCopyrightCommentInternal(text, startIndex, textLength, matchStartIndex, matchEndIndex);
}

int String8Native::SplitAlphanumeric(Byte* text, Int32 startIndex, Int32 textLength, Int32* outWordBoundaries, Int32 outDelimiterLengthLimit)
{
	return SplitAlphanumericInternal(text, startIndex, textLength, outWordBoundaries, outDelimiterLengthLimit);
}