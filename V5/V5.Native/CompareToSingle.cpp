#include "stdafx.h"
#include "Operator.h"
#include "CompareToVector.h"

#pragma unmanaged

template<CompareOperatorN cOp, BooleanOperatorN bOp, typename T>
static void WhereSingle(T* set, int length, T value, unsigned __int64* matchVector)
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
			case CompareOperatorN::Equals:
				if (set[i] == value) result |= (0x1ULL << (i & 63));
				break;
			case CompareOperatorN::NotEquals:
				if (set[i] != value) result |= (0x1ULL << (i & 63));
				break;
			}
		}

		switch (bOp)
		{
		case BooleanOperatorN::Set:
			matchVector[vectorIndex] = result;
			break;
		case BooleanOperatorN::And:
			matchVector[vectorIndex] &= result;
			break;
		case BooleanOperatorN::Or:
			matchVector[vectorIndex] |= result;
			break;
		case BooleanOperatorN::AndNot:
			matchVector[vectorIndex] &= ~result;
			break;
		}
	}
}

template<BooleanOperatorN bOp, typename T>
void CompareToVector::WhereSingleB(CompareOperatorN cOp, T* set, int length, T value, unsigned __int64* matchVector)
{
	switch (cOp)
	{
	case CompareOperatorN::GreaterThan:
		WhereSingle<CompareOperatorN::GreaterThan, bOp>(set, length, value, matchVector);
		break;
	case CompareOperatorN::GreaterThanOrEqual:
		WhereSingle<CompareOperatorN::GreaterThanOrEqual, bOp>(set, length, value, matchVector);
		break;
	case CompareOperatorN::LessThan:
		WhereSingle<CompareOperatorN::LessThan, bOp>(set, length, value, matchVector);
		break;
	case CompareOperatorN::LessThanOrEqual:
		WhereSingle<CompareOperatorN::LessThanOrEqual, bOp>(set, length, value, matchVector);
		break;
	case CompareOperatorN::Equals:
		WhereSingle<CompareOperatorN::Equals, bOp>(set, length, value, matchVector);
		break;
	case CompareOperatorN::NotEquals:
		WhereSingle<CompareOperatorN::NotEquals, bOp>(set, length, value, matchVector);
		break;
	}
}

template<typename T>
void CompareToVector::WhereSingle(CompareOperatorN cOp, BooleanOperatorN bOp, T* set, int length, T value, unsigned __int64* matchVector)
{
	switch (bOp)
	{
	case BooleanOperatorN::And:
		WhereSingleB<BooleanOperatorN::And, T>(cOp, set, length, value, matchVector);
		break;
	case BooleanOperatorN::Or:
		WhereSingleB<BooleanOperatorN::Or, T>(cOp, set, length, value, matchVector);
		break;
	case BooleanOperatorN::AndNot:
		WhereSingleB<BooleanOperatorN::AndNot, T>(cOp, set, length, value, matchVector);
		break;
	case BooleanOperatorN::Set:
		WhereSingleB<BooleanOperatorN::Set, T>(cOp, set, length, value, matchVector);
		break;
	}
}