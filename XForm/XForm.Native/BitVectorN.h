// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class BitVectorN
		{
		public:
			static Int32 Count(array<UInt64>^ vector);
			static Int32 Page(array<UInt64>^ vector, array<Int32>^ indicesFound, Int32% fromIndex, Int32 countLimit);
		};
	}
}
