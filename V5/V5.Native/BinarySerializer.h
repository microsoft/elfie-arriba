#pragma once
using namespace System;
using namespace System::IO;

namespace V5
{
	namespace Serialization
	{
		public ref class BinarySerializer
		{
		private:
			generic <typename T>
			static String^ TypeIdentifier();

		public:
			generic <typename T>
			static array<T>^ Read(String^ filePath);

			generic <typename T>
			static void Write(String^ filePath, array<T>^ set);

			generic <typename T>
			static void Write(String^ filePath, array<T>^ set, int index, int length);

			generic <typename T>
			static array<T>^ ReadArray(BinaryReader^ reader, __int64 lengthBytes);

			generic<typename T>
			static void Write(BinaryWriter^ writer, array<T>^ set);

			generic<typename T>
			static void Write(BinaryWriter^ writer, array<T>^ set, int index, int length);
		};
	}
}

