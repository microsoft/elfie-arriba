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
		};
	}
}
