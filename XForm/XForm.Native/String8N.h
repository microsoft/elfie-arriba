// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class String8N
		{
		public:
			static Int32 SplitTsv(array<Byte>^ content, Int32 index, Int32 length, array<UInt64>^ cellVector, array<UInt64>^ rowVector);
			static Int32 IndexOfAll(array<Byte>^ content, Int32 index, Int32 length, array<Byte>^ value, Int32 valueIndex, Int32 valueLength, Boolean ignoreCase, array<Int32>^ matchArray);
		};
	}
}
