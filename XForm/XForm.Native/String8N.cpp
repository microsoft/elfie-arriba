#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "String8N.h"

#pragma unmanaged
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
	}
}
