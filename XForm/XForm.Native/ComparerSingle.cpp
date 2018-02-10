// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Operator.h"
#include "Comparer.h"

#pragma unmanaged

template<CompareOperatorN cOp, typename T>
static void WhereSingle(T* set, int length, T value, BooleanOperatorN bOp, unsigned __int64* matchVector)
{
	int vectorLength = (length + 63) >> 6;
	for (int vectorIndex = 0; vectorIndex < vectorLength; ++vectorIndex)
	{
		unsigned __int64 result = 0;

		int i = vectorIndex << 6;
		int end = (vectorIndex + 1) << 6;
		if (length < end) end = length;

		for (; i < end; ++i)
		{
			switch (cOp)
			{
			case CompareOperatorN::GreaterThan:
				if (set[i] > value) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::GreaterThanOrEqual:
				if (set[i] >= value) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::LessThan:
				if (set[i] < value) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::LessThanOrEqual:
				if (set[i] <= value) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::Equal:
				if (set[i] == value) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::NotEqual:
				if (set[i] != value) result |= (0x1ULL << (i & 63));
				break;
			}
		}

		switch (bOp)
		{
		case BooleanOperatorN::And:
			matchVector[vectorIndex] &= result;
			break;
		case BooleanOperatorN::Or:
			matchVector[vectorIndex] |= result;
			break;
		}
	}
}

template<CompareOperatorN cOp, typename T>
static void WhereSingle(T* left, int length, T* right, BooleanOperatorN bOp, unsigned __int64* matchVector)
{
	int vectorLength = (length + 63) >> 6;
	for (int vectorIndex = 0; vectorIndex < vectorLength; ++vectorIndex)
	{
		unsigned __int64 result = 0;

		int i = vectorIndex << 6;
		int end = (vectorIndex + 1) << 6;
		if (length < end) end = length;

		for (; i < end; ++i)
		{
			switch (cOp)
			{
			case CompareOperatorN::GreaterThan:
				if (left[i] > right[i]) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::GreaterThanOrEqual:
				if (left[i] >= right[i]) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::LessThan:
				if (left[i] < right[i]) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::LessThanOrEqual:
				if (left[i] <= right[i]) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::Equal:
				if (left[i] == right[i]) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::NotEqual:
				if (left[i] != right[i]) result |= (0x1ULL << (i & 63));
				break;
			}
		}

		switch (bOp)
		{
		case BooleanOperatorN::And:
			matchVector[vectorIndex] &= result;
			break;
		case BooleanOperatorN::Or:
			matchVector[vectorIndex] |= result;
			break;
		}
	}
}

#pragma managed

namespace XForm
{
	namespace Native
	{
		template<typename T>
		void Comparer::WhereSingle(T* set, int length, Byte cOp, T value, Byte bOp, unsigned __int64* matchVector)
		{
			switch (cOp)
			{
			case CompareOperatorN::GreaterThan:
				WhereSingle<CompareOperatorN::GreaterThan>(set, length, value, bOp, matchVector);
				break;
			case CompareOperatorN::GreaterThanOrEqual:
				WhereSingle<CompareOperatorN::GreaterThanOrEqual>(set, length, value, bOp, matchVector);
				break;
			case CompareOperatorN::LessThan:
				WhereSingle<CompareOperatorN::LessThan>(set, length, value, bOp, matchVector);
				break;
			case CompareOperatorN::LessThanOrEqual:
				WhereSingle<CompareOperatorN::LessThanOrEqual>(set, length, value, bOp, matchVector);
				break;
			case CompareOperatorN::Equals:
				WhereSingle<CompareOperatorN::Equals>(set, length, value, bOp, matchVector);
				break;
			case CompareOperatorN::NotEquals:
				WhereSingle<CompareOperatorN::NotEquals>(set, length, value, bOp, matchVector);
				break;
			}
		}

		template<typename T>
		void Comparer::WhereSingle(T* left, int length, Byte cOp, T* right, Byte bOp, unsigned __int64* matchVector)
		{
			switch (cOp)
			{
			case CompareOperatorN::GreaterThan:
				WhereSingle<CompareOperatorN::GreaterThan>(left, length, right, bOp, matchVector);
				break;
			case CompareOperatorN::GreaterThanOrEqual:
				WhereSingle<CompareOperatorN::GreaterThanOrEqual>(left, length, right, bOp, matchVector);
				break;
			case CompareOperatorN::LessThan:
				WhereSingle<CompareOperatorN::LessThan>(left, length, right, bOp, matchVector);
				break;
			case CompareOperatorN::LessThanOrEqual:
				WhereSingle<CompareOperatorN::LessThanOrEqual>(left, length, right, bOp, matchVector);
				break;
			case CompareOperatorN::Equals:
				WhereSingle<CompareOperatorN::Equals>(left, length, right, bOp, matchVector);
				break;
			case CompareOperatorN::NotEquals:
				WhereSingle<CompareOperatorN::NotEquals>(left, length, right, bOp, matchVector);
				break;
			}
		}
	}
}